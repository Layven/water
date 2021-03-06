﻿using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpHelper;

namespace Water {

    public class Water : IDisposable {

        //Indices
        private int[] indices;

        private int N = 100, M = 100;
        private Vector3 botLeftCorner = new Vector3(-10.0f, -2.0f, -6.0f);
        private float size = 20;

        public Wave wave = new Wave(2.0f,0.7f);

        //Vertices
        private ColoredVertex[] vertices;

        private SharpMesh mesh;

        public Water(SharpDevice device) {

            vertices = new ColoredVertex[N * M];
            for (int i = 0; i < M; i++) {
                for (int j = 0; j < N; j++) {
                    vertices[i * N + j] = new ColoredVertex(
                        new Vector3(i*size/N+ botLeftCorner.X, 0 + botLeftCorner.Y, j*size/M + botLeftCorner.Z),
                        new Vector4((i + 0.0f) / M, (j + 0.0f) / N, 0, 1)
                    );
                }
            }
            
            indices = new int[6 * (N - 1) * (M - 1)];
            for (int i = 0; i < M - 1; i++) {
                for (int j = 0; j < N - 1; j++) {
                    int act = i * (N - 1) + j;
                    int fullIndex = i * N + j;

                    indices[6 * act + 0] = fullIndex;
                    indices[6 * act + 1] = fullIndex + 1;
                    indices[6 * act + 2] = fullIndex + N;
                    indices[6 * act + 3] = fullIndex + 1;
                    indices[6 * act + 4] = fullIndex + N + 1;
                    indices[6 * act + 5] = fullIndex + N;
                }
            }

            this.createMesh(device);

        }

        public SharpMesh createMesh(SharpDevice device) {
            this.mesh = SharpMesh.Create<ColoredVertex>(device, vertices, indices);
            return mesh;
        }
        
        public void Draw() {
            mesh.Draw();
        }

        public void Dispose() {
            mesh?.Dispose();
        }
    }

    public class Wave {
        public float speed;
        public float wavelength;
        public float amplitude;
        public Vector4 waveDir;
        public float gravity = 9.81f;

        public Wave(float wavelength, float amplitude) {
            this.wavelength = wavelength;
            this.amplitude = amplitude;
            speed = (float) Math.Sqrt(gravity*wavelength/(2.0*Math.PI));
            waveDir = new Vector4(2.0f,0.0f,-0.0f, 0.0f);
            waveDir.Normalize();
        }


    }
}
