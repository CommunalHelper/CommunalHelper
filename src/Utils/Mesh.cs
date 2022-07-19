using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Utils {
    /// <summary>
    /// Small utility class to handle groups of vertices and indices, and draw them.<br/>
    /// Was originally written for another mod.
    /// </summary>
    /// <typeparam name="T">The <see cref="IVertexType"/> used in this mesh.</typeparam>
    public abstract class Mesh<T> : IDisposable where T : struct, IVertexType {
        private List<T> _vertices = new();
        private List<int> _indices = new();

        /// <summary>
        /// The array of vertices.<br/>
        /// Remains <c>null</c> until this mesh is baked.
        /// </summary>
        public T[] Vertices { get; private set; }

        /// <summary>
        /// The array of indices.<br/>
        /// Remains <c>null</c> until this mesh is baked.
        /// </summary>
        public int[] Indices { get; private set; }

        /// <summary>
        /// The current amount of vertices in this mesh.
        /// </summary>
        public int VertexCount => Baked ? Vertices.Length : _vertices.Count;

        /// <summary>
        /// The current amount of triangles in this mesh.<br/>
        /// Always the number of indices divided by 3.
        /// </summary>
        public int Triangles { get; private set; }

        /// <summary>
        /// Whether this mesh has been baked or not.
        /// </summary>
        public bool Baked { get; private set; }

        private readonly Effect effect;

        /// <summary>
        /// Creates a new mesh.
        /// </summary>
        /// <param name="effect">The effect used to draw vertices.</param>
        public Mesh(Effect effect) {
            this.effect = effect;
        }

        /// <summary>
        /// Adds a single vertex to this mesh.<br/>
        /// This can only be done if the mesh is unbaked.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void AddVertex(T vertex) {
            if (Baked)
                throw new InvalidOperationException("Cannot add a vertex to a baked mesh!");

            _vertices.Add(vertex);
        }

        /// <summary>
        /// Adds an array of vertices to this mesh.<br/>
        /// This can only be done if the mesh is unbaked.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void AddVertices(params T[] vertices) {
            if (Baked)
                throw new InvalidOperationException("Cannot add vertices to a baked mesh!");

            _vertices.AddRange(vertices);
        }

        /// <summary>
        /// Adds a triangle to this mesh given the three indices of the triangle's vertices.<br/>
        /// This can only be done if the mesh is unbaked.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void AddTriangle(int a, int b, int c) {
            if (Baked)
                throw new InvalidOperationException("Cannot add indices to a baked mesh!");

            _indices.Add(a);
            _indices.Add(b);
            _indices.Add(c);

            ++Triangles;
        }

        /// <summary>
        /// Creates the <see cref="Vertices"/> and <see cref="Indices"/> arrays that will be used for drawing.<br/>
        /// This can only be called once, and makes this mesh uneditable.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void Bake() {
            if (Baked)
                throw new InvalidOperationException("Cannot bake mesh that was already baked!");

            Vertices = _vertices.ToArray();
            Indices = _indices.ToArray();

            Baked = true;
        }

        /// <summary>
        /// Draws the vertices through <see cref="GFX.DrawIndexedVertices{T}(Matrix, T[], int, int[], int, Effect, BlendState)"/>.<br/>
        /// This can only be done if the mesh is baked.
        /// </summary>
        /// <param name="matrix"></param>
        public void Celeste_DrawVertices(Matrix? matrix = null) {
            if (!Baked)
                throw new InvalidOperationException("A mesh must be baked in order for its vertices to be drawn!");

            GFX.DrawIndexedVertices(matrix ?? Matrix.Identity, Vertices, Vertices.Length, Indices, Triangles, effect);
        }

        public void Dispose() {
            _vertices = null;
            Vertices = null;
            _indices = null;
            Indices = null;
        }
    }
}
