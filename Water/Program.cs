using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using SharpHelper;
using System.Windows.Forms;

using Buffer11 = SharpDX.Direct3D11.Buffer;

namespace Water {
    class Program {

        private const int count = 3;
        [StructLayout(LayoutKind.Explicit,Size = 144)]
        struct UniformData {
            [FieldOffset(0)]
            public Matrix worldViewProj;
            [FieldOffset(64)]
            public Matrix worldView;
            [FieldOffset(128)]
            public float time;
        }

        [StructLayout(LayoutKind.Explicit,Size = 3*16*count)]
        struct ArrayData {
            [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = count)]
            public Vector4[] array;
            [FieldOffset(16*count), MarshalAs(UnmanagedType.ByValArray, SizeConst = count)]
            public Vector4[] waveDir;

            [FieldOffset(2*16*count), MarshalAs(UnmanagedType.ByValArray, SizeConst = count)]
            public Vector4[] waveStats;

        }

        static byte[] getBytes(ArrayData str) {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        static ArrayData fromBytes(byte[] arr) {
            ArrayData str = new ArrayData();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (ArrayData) Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }

        static void Main(string[] args) {

            if(!SharpDevice.IsDirectX11Supported()) {
                System.Windows.Forms.MessageBox.Show("DirectX11 Not Supported");
                return;
            }

            //render form
            RenderForm form = new RenderForm();
            form.Text = "Tutorial 4: Primitives";

            //Help to count Frame Per Seconds
            SharpFPS fpsCounter = new SharpFPS();

            using(SharpDevice device = new SharpDevice(form)) {

                //Init Mesh
                Water water = new Water(device);

                //Create Shader From File and Create Input Layout
                SharpShader shader = new SharpShader(device, "../../HLSL.txt",
                    new SharpShaderDescription() { VertexShaderFunction = "VS", PixelShaderFunction = "PS" },
                    new InputElement[] {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
                    });

                //create constant buffer
                Buffer11 buffer = shader.CreateBuffer<UniformData>();
                Buffer11 arrayBuffer = shader.CreateBuffer<ArrayData>();


                fpsCounter.Reset();

                form.KeyDown += (sender, e) =>
                {
                    switch (e.KeyCode)
                    {
                        case Keys.W:
                            device.SetWireframeRasterState();
                            device.SetDefaultBlendState();
                            break;
                        case Keys.S:
                            device.SetDefaultRasterState();
                            break;
                        case Keys.D1:
                            device.SetDefaultBlendState();
                            break;
                        case Keys.D2:
                            device.SetBlend(BlendOperation.Add, BlendOption.InverseSourceAlpha, BlendOption.SourceAlpha);
                            break;
                        case Keys.D3:
                            device.SetBlend(BlendOperation.Add, BlendOption.SourceAlpha, BlendOption.InverseSourceAlpha);
                            break;
                        case Keys.D4:
                            device.SetBlend(BlendOperation.Add, BlendOption.SourceColor, BlendOption.InverseSourceColor);
                            break;
                        case Keys.D5:
                            device.SetBlend(BlendOperation.Add, BlendOption.SourceColor, BlendOption.DestinationColor);
                            break;
                    }
                };

                //main loop
                RenderLoop.Run(form, () => {
                    //Resizing
                    if(device.MustResize) {
                        device.Resize();
                    }

                    //apply states
                    device.UpdateAllStates();

                    //clear color
                    device.Clear(Color.CornflowerBlue);

                    //apply shader
                    shader.Apply();

                    //Set matrices
                    float ratio = (float) form.ClientRectangle.Width / (float) form.ClientRectangle.Height;
                    Matrix projection = Matrix.PerspectiveFovLH(3.14F / 3.0F, ratio, 1, 1000);
                    Matrix view = Matrix.LookAtLH(new Vector3(0, 12, -12), new Vector3(), Vector3.UnitY);
                    Matrix world = Matrix.RotationY(Environment.TickCount / 1000.0F * 0.1f);
                    world = Matrix.Identity;


                    float time = Environment.TickCount / 1000.0F;
                    Vector4[] waveStats = new Vector4[count] {
                        new Vector4(water.wave.speed*1.0f,water.wave.wavelength*1.0f,water.wave.amplitude*1.0f,0),
                        new Vector4(water.wave.speed*1.0f,water.wave.wavelength*1.0f,water.wave.amplitude*1.0f,0),
                        new Vector4(water.wave.speed*1.0f,water.wave.wavelength*1.0f,water.wave.amplitude*1.0f,0),
                    };

                    Vector4 tmp = new Vector4(2.0f,0.0f,-2.0f,0.0f);
                    tmp.Normalize();

                    Vector4[] waveDir = new Vector4[count] {
                        water.wave.waveDir,
                        new Vector4(water.wave.waveDir.Z, 0, -water.wave.waveDir.X, 0),
                        tmp,
                    };

                    var array = new Vector4[count] {
                        new Vector4(1.0f,0.0f,0.0f,0.0f),
                        new Vector4(1.0f,1.0f,1.0f,0.0f),
                        new Vector4(1.0f,1.0f,1.0f,0.0f),
                    };

                    water.wave.waveDir = new Vector4((float) Math.Sin(time / 5.0), 0, (float) Math.Cos(time / 5.0), 0);

                    UniformData sceneInfo = new UniformData() {
                        worldViewProj = world * view * projection,
                        worldView = world * view,
                        time = time,
                    };

                    ArrayData arrayInfo = new ArrayData() {
                        array = array,
                        waveDir = waveDir,
                        waveStats = waveStats,
                    };


                    using (DataStream ds = new DataStream(Utilities.SizeOf<ArrayData>(),true,true)) {
                        //device.UpdateDataWithDataStream(arrayBuffer,ds);
                        ds.WriteRange(getBytes(arrayInfo));
                        ds.Position = 0;
                        arrayBuffer.Dispose();
                        arrayBuffer = shader.CreateBuffer<ArrayData>(ds);
                        //device.UnmapDataStream(arrayBuffer);
                        
                    }

                    //update constant buffer
                    device.UpdateData<UniformData>(buffer, sceneInfo);

                    //pass constant buffer to shader
                    device.DeviceContext.VertexShader.SetConstantBuffer(0, buffer);
                    device.DeviceContext.PixelShader.SetConstantBuffer(0, buffer);
                    device.DeviceContext.VertexShader.SetConstantBuffer(1, arrayBuffer);


                    //draw mesh
                    water.Draw();

                    //begin drawing text
                    device.Font.Begin();

                    //draw string
                    fpsCounter.Update();
                    device.Font.DrawString("FPS: " + fpsCounter.FPS, 0, 0);

                    //flush text to view
                    device.Font.End();
                    //present
                    device.Present();
                });

                //release resources
                water.Dispose();
                buffer.Dispose();
            }



        }
    }
}
