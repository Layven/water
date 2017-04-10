using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using SharpHelper;

using Buffer11 = SharpDX.Direct3D11.Buffer;

namespace Water {
    class Program {

        struct UniformData {
            public Matrix worldViewProj;
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
                    Matrix view = Matrix.LookAtLH(new Vector3(0, 4, -10), new Vector3(), Vector3.UnitY);
                    Matrix world = Matrix.RotationX(Environment.TickCount / 1000.0F);
                    world = Matrix.Identity;
                    Matrix WVP = world * view * projection;

                    Vector3 phase = new Vector3(Environment.TickCount / 1000.0F, 0, 0);

                    UniformData sceneInfo = new UniformData() {
                        worldViewProj = WVP
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
