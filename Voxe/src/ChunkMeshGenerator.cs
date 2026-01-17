using System;
using OpenTK.Mathematics;
using Voxe.Math;
using Vertex = Voxe.ChunkMesh.Vertex;

namespace Voxe;

public class ChunkMeshGenerator
{
	// Num blocks * num sides * num vertices/indices per side
	private const int MaxNumVertices = Chunk.NumBlocks * 6 * 4;
	private const int MaxNumIndices = Chunk.NumBlocks * 6 * 6;

	private readonly Vertex[] _vertices = new Vertex[MaxNumVertices];
	private readonly uint[] _indices = new uint[MaxNumIndices];

	public readonly ref struct Result(
		ReadOnlySpan<Vertex> vertices,
		ReadOnlySpan<uint> indices)
	{
		public ReadOnlySpan<Vertex> Vertices { get; } = vertices;
		public ReadOnlySpan<uint> Indices { get; } = indices;
	}

	public Result GenerateMesh(Chunk chunk)
	{
		uint numVertices = 0;
		uint numIndices = 0;

		for (int y = 0; y < Chunk.ChunkHeight; y++)
		{
			for (int z = 0; z < Chunk.ChunkWidth; z++)
			{
				for (int x = 0; x < Chunk.ChunkWidth; x++)
				{
					Block block = chunk.GetBlock(x, y, z);
					BlockType type = BlocksRegistry.GetBlockType(block.TypeId);
					if (type.IsInvisible)
					{
						continue;
					}

					UvSet uvSet = BlocksRegistry.GetBlockUvSet(block.TypeId);

					float nx = x;
					float ny = y;
					float nz = z;
					float px = x + 1;
					float py = y + 1;
					float pz = z + 1;

					Rect uvRect;

					// +x
					uvRect = uvSet.PositiveX;
					_vertices[numVertices + 0] = new Vertex(new Vector3(px, py, pz), uvRect.TopLeft);
					_vertices[numVertices + 1] = new Vertex(new Vector3(px, ny, pz), uvRect.BottomLeft);
					_vertices[numVertices + 2] = new Vertex(new Vector3(px, ny, nz), uvRect.BottomRight);
					_vertices[numVertices + 3] = new Vertex(new Vector3(px, py, nz), uvRect.TopRight);
					AppendIndices(ref numVertices, ref numIndices);

					// -x
					uvRect = uvSet.NegativeX;
					_vertices[numVertices + 0] = new Vertex(new Vector3(nx, py, nz), uvRect.TopLeft);
					_vertices[numVertices + 1] = new Vertex(new Vector3(nx, ny, nz), uvRect.BottomLeft);
					_vertices[numVertices + 2] = new Vertex(new Vector3(nx, ny, pz), uvRect.BottomRight);
					_vertices[numVertices + 3] = new Vertex(new Vector3(nx, py, pz), uvRect.TopRight);
					AppendIndices(ref numVertices, ref numIndices);

					// +z
					uvRect = uvSet.PositiveZ;
					_vertices[numVertices + 0] = new Vertex(new Vector3(nx, py, pz), uvRect.TopLeft);
					_vertices[numVertices + 1] = new Vertex(new Vector3(nx, ny, pz), uvRect.BottomLeft);
					_vertices[numVertices + 2] = new Vertex(new Vector3(px, ny, pz), uvRect.BottomRight);
					_vertices[numVertices + 3] = new Vertex(new Vector3(px, py, pz), uvRect.TopRight);
					AppendIndices(ref numVertices, ref numIndices);

					// -z
					uvRect = uvSet.NegativeZ;
					_vertices[numVertices + 0] = new Vertex(new Vector3(px, py, nz), uvRect.TopLeft);
					_vertices[numVertices + 1] = new Vertex(new Vector3(px, ny, nz), uvRect.BottomLeft);
					_vertices[numVertices + 2] = new Vertex(new Vector3(nx, ny, nz), uvRect.BottomRight);
					_vertices[numVertices + 3] = new Vertex(new Vector3(nx, py, nz), uvRect.TopRight);
					AppendIndices(ref numVertices, ref numIndices);

					// +y
					uvRect = uvSet.PositiveY;
					_vertices[numVertices + 0] = new Vertex(new Vector3(px, py, nz), uvRect.TopLeft);
					_vertices[numVertices + 1] = new Vertex(new Vector3(nx, py, nz), uvRect.BottomLeft);
					_vertices[numVertices + 2] = new Vertex(new Vector3(nx, py, pz), uvRect.BottomRight);
					_vertices[numVertices + 3] = new Vertex(new Vector3(px, py, pz), uvRect.TopRight);
					AppendIndices(ref numVertices, ref numIndices);

					// -y
					uvRect = uvSet.NegativeY;
					_vertices[numVertices + 0] = new Vertex(new Vector3(px, ny, pz), uvRect.TopLeft);
					_vertices[numVertices + 1] = new Vertex(new Vector3(nx, ny, pz), uvRect.BottomLeft);
					_vertices[numVertices + 2] = new Vertex(new Vector3(nx, ny, nz), uvRect.BottomRight);
					_vertices[numVertices + 3] = new Vertex(new Vector3(px, ny, nz), uvRect.TopRight);
					AppendIndices(ref numVertices, ref numIndices);
				}
			}
		}

		ReadOnlySpan<Vertex> vertices = new(_vertices, 0, (int)numVertices);
		ReadOnlySpan<uint> indices = new(_indices, 0, (int)numIndices);
		return new Result(vertices, indices);
	}

	private void AppendIndices(ref uint numVertices, ref uint numIndices)
	{
		_indices[numIndices + 0] = numVertices + 0;
		_indices[numIndices + 1] = numVertices + 1;
		_indices[numIndices + 2] = numVertices + 2;

		_indices[numIndices + 3] = numVertices + 2;
		_indices[numIndices + 4] = numVertices + 3;
		_indices[numIndices + 5] = numVertices + 0;

		numVertices += 4;
		numIndices += 6;
	}
}