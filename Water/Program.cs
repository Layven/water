using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using SharpHelper;
using System.Windows.Forms;

using Buffer11 = SharpDX.Direct3D11.Buffer;

namespace Water {
    class Program {

        private const int count = 2;
        [StructLayout(LayoutKind.Explicit,Size = 128)]
        struct UniformData {
            [FieldOffset(0)]
            public Matrix worldViewProj;
            [FieldOffset(64)]
            public float time;
            [FieldOffset(68)]
            public float speed;
            [FieldOffset(72)]
            public float wavelength;
            [FieldOffset(76)]
            public float amplitude;
            [FieldOffset(80)]
            public Vector4 waveDir;
            [FieldOffset(96), MarshalAs(UnmanagedType.ByValArray, SizeConst = count)]
            public Vector4[] array;
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
                    Matrix WVP = world * view * projection;


                    float time = Environment.TickCount / 1000.0F;
                    float speed = water.wave.speed;
                    float wavelength = water.wave.wavelenght;
                    float amplitude = water.wave.amplitude;

                    UniformData sceneInfo = new UniformData() {
                        worldViewProj = WVP,
                        time = time,
                        speed = speed,
                        wavelength = wavelength,
                        amplitude = amplitude,
                        waveDir = water.wave.waveDir,
                        array = new Vector4[count] {
                            new Vector4(1.0f,1.0f,1.0f,1.0f),
                            new Vector4(1.0f,1.0f,1.0f,1.0f)
                        }
                    };

                    //update constant buffer
                    device.UpdateData<UniformData>(buffer, sceneInfo);

                    //pass constant buffer to shader
                    device.DeviceContext.VertexShader.SetConstantBuffer(0, buffer);

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
