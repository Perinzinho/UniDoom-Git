using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

[ScriptedImporter(1, "glb")]
public sealed class GlbVertexColorImporter : ScriptedImporter
{
    const uint JsonChunk = 0x4E4F534A;
    const uint BinaryChunk = 0x004E4942;
    const string ShaderPath = "Assets/_Project/Scenes/CampusLibrary/Shaders/GeneratorVertexColor.shader";

    public override void OnImportAsset(AssetImportContext context)
    {
        byte[] file = File.ReadAllBytes(context.assetPath);
        if (file.Length < 20 || BitConverter.ToUInt32(file, 0) != 0x46546C67 || BitConverter.ToUInt32(file, 4) != 2)
            throw new InvalidDataException("Only binary glTF 2.0 files are supported.");

        string json = null;
        byte[] binary = null;
        int offset = 12;
        while (offset + 8 <= file.Length)
        {
            int length = checked((int)BitConverter.ToUInt32(file, offset));
            uint type = BitConverter.ToUInt32(file, offset + 4);
            offset += 8;
            if (offset + length > file.Length) throw new InvalidDataException("Invalid GLB chunk length.");
            if (type == JsonChunk) json = Encoding.UTF8.GetString(file, offset, length).TrimEnd('\0', ' ', '\r', '\n');
            else if (type == BinaryChunk)
            {
                binary = new byte[length];
                Buffer.BlockCopy(file, offset, binary, 0, length);
            }
            offset += length;
        }

        if (string.IsNullOrEmpty(json) || binary == null) throw new InvalidDataException("GLB must contain JSON and BIN chunks.");
        GltfRoot data = JsonUtility.FromJson<GltfRoot>(json);
        if (data?.nodes == null || data.meshes == null || data.accessors == null || data.bufferViews == null)
            throw new InvalidDataException("GLB scene data is incomplete.");

        var vertices = new List<Vector3>(8192);
        var colors = new List<Color32>(8192);
        var triangles = new List<int>(16384);
        int sceneIndex = Mathf.Clamp(data.scene, 0, data.scenes.Length - 1);
        foreach (int rootNode in data.scenes[sceneIndex].nodes)
            AppendNode(data, binary, rootNode, Matrix4x4.identity, vertices, colors, triangles);

        var mesh = new Mesh
        {
            name = Path.GetFileNameWithoutExtension(context.assetPath),
            indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0, true);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (!shader) throw new InvalidOperationException("Generator vertex-color shader is missing.");
        context.DependsOnSourceAsset(ShaderPath);
        var material = new Material(shader) { name = mesh.name + " vertex colors" };

        var root = new GameObject(mesh.name);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterial = material;

        context.AddObjectToAsset("mesh", mesh);
        context.AddObjectToAsset("material", material);
        context.AddObjectToAsset("main", root);
        context.SetMainObject(root);
    }

    static void AppendNode(GltfRoot data, byte[] binary, int nodeIndex, Matrix4x4 parent,
        List<Vector3> vertices, List<Color32> colors, List<int> triangles)
    {
        if (nodeIndex < 0 || nodeIndex >= data.nodes.Length) return;
        NodeDef node = data.nodes[nodeIndex];
        Matrix4x4 world = parent * LocalMatrix(node);

        if (node.mesh >= 0 && node.mesh < data.meshes.Length)
        {
            foreach (PrimitiveDef primitive in data.meshes[node.mesh].primitives)
            {
                if (primitive.mode != 4) throw new NotSupportedException("Only triangle GLB primitives are supported.");
                Vector3[] sourcePositions = ReadPositions(data, binary, primitive.attributes.POSITION);
                Color32[] sourceColors = ReadColors(data, binary, primitive.attributes.COLOR_0, sourcePositions.Length);
                int[] sourceIndices = ReadIndices(data, binary, primitive.indices);
                int vertexBase = vertices.Count;

                for (int i = 0; i < sourcePositions.Length; i++)
                {
                    Vector3 source = world.MultiplyPoint3x4(sourcePositions[i]);
                    vertices.Add(new Vector3(source.x, source.z, -source.y));
                    colors.Add(sourceColors[i]);
                }

                // glTF is counter-clockwise; Unity uses clockwise front faces.
                for (int i = 0; i + 2 < sourceIndices.Length; i += 3)
                {
                    triangles.Add(vertexBase + sourceIndices[i]);
                    triangles.Add(vertexBase + sourceIndices[i + 2]);
                    triangles.Add(vertexBase + sourceIndices[i + 1]);
                }
            }
        }

        if (node.children != null)
            foreach (int child in node.children) AppendNode(data, binary, child, world, vertices, colors, triangles);
    }

    static Matrix4x4 LocalMatrix(NodeDef node)
    {
        if (node.matrix != null && node.matrix.Length == 16)
        {
            var result = new Matrix4x4();
            for (int column = 0; column < 4; column++)
            for (int row = 0; row < 4; row++) result[row, column] = node.matrix[column * 4 + row];
            return result;
        }
        Vector3 position = node.translation != null && node.translation.Length == 3 ?
            new Vector3(node.translation[0], node.translation[1], node.translation[2]) : Vector3.zero;
        Quaternion rotation = node.rotation != null && node.rotation.Length == 4 ?
            new Quaternion(node.rotation[0], node.rotation[1], node.rotation[2], node.rotation[3]) : Quaternion.identity;
        Vector3 scale = node.scale != null && node.scale.Length == 3 ?
            new Vector3(node.scale[0], node.scale[1], node.scale[2]) : Vector3.one;
        return Matrix4x4.TRS(position, rotation, scale);
    }

    static Vector3[] ReadPositions(GltfRoot data, byte[] binary, int accessorIndex)
    {
        AccessorDef accessor = data.accessors[accessorIndex];
        if (accessor.componentType != 5126 || accessor.type != "VEC3")
            throw new NotSupportedException("Positions must use FLOAT VEC3 accessors.");
        BufferViewDef view = data.bufferViews[accessor.bufferView];
        int stride = view.byteStride > 0 ? view.byteStride : 12;
        int start = view.byteOffset + accessor.byteOffset;
        var values = new Vector3[accessor.count];
        for (int i = 0; i < values.Length; i++)
        {
            int at = start + i * stride;
            values[i] = new Vector3(BitConverter.ToSingle(binary, at), BitConverter.ToSingle(binary, at + 4), BitConverter.ToSingle(binary, at + 8));
        }
        return values;
    }

    static Color32[] ReadColors(GltfRoot data, byte[] binary, int accessorIndex, int count)
    {
        var values = new Color32[count];
        if (accessorIndex < 0)
        {
            for (int i = 0; i < count; i++) values[i] = new Color32(255, 255, 255, 255);
            return values;
        }
        AccessorDef accessor = data.accessors[accessorIndex];
        BufferViewDef view = data.bufferViews[accessor.bufferView];
        int components = accessor.type == "VEC3" ? 3 : 4;
        int componentSize = accessor.componentType == 5126 ? 4 : accessor.componentType == 5123 ? 2 : 1;
        int stride = view.byteStride > 0 ? view.byteStride : components * componentSize;
        int start = view.byteOffset + accessor.byteOffset;
        for (int i = 0; i < count; i++)
        {
            int at = start + i * stride;
            byte r = ColorByte(binary, at, accessor.componentType);
            byte g = ColorByte(binary, at + componentSize, accessor.componentType);
            byte b = ColorByte(binary, at + componentSize * 2, accessor.componentType);
            byte a = components == 4 ? ColorByte(binary, at + componentSize * 3, accessor.componentType) : (byte)255;
            values[i] = new Color32(r, g, b, a);
        }
        return values;
    }

    static byte ColorByte(byte[] data, int offset, int componentType)
    {
        if (componentType == 5121) return data[offset];
        if (componentType == 5123) return (byte)Mathf.RoundToInt(BitConverter.ToUInt16(data, offset) / 65535f * 255f);
        if (componentType == 5126) return (byte)Mathf.RoundToInt(Mathf.Clamp01(BitConverter.ToSingle(data, offset)) * 255f);
        throw new NotSupportedException("Unsupported vertex-color component type.");
    }

    static int[] ReadIndices(GltfRoot data, byte[] binary, int accessorIndex)
    {
        AccessorDef accessor = data.accessors[accessorIndex];
        BufferViewDef view = data.bufferViews[accessor.bufferView];
        int size = accessor.componentType == 5125 ? 4 : accessor.componentType == 5123 ? 2 : 1;
        int stride = view.byteStride > 0 ? view.byteStride : size;
        int start = view.byteOffset + accessor.byteOffset;
        var values = new int[accessor.count];
        for (int i = 0; i < values.Length; i++)
        {
            int at = start + i * stride;
            values[i] = accessor.componentType == 5125 ? checked((int)BitConverter.ToUInt32(binary, at)) :
                accessor.componentType == 5123 ? BitConverter.ToUInt16(binary, at) : binary[at];
        }
        return values;
    }

    [Serializable] sealed class GltfRoot
    {
        public int scene;
        public SceneDef[] scenes;
        public NodeDef[] nodes;
        public MeshDef[] meshes;
        public AccessorDef[] accessors;
        public BufferViewDef[] bufferViews;
    }
    [Serializable] sealed class SceneDef { public int[] nodes; }
    [Serializable] sealed class NodeDef
    {
        public string name;
        public int mesh = -1;
        public int[] children;
        public float[] matrix, translation, rotation, scale;
    }
    [Serializable] sealed class MeshDef { public string name; public PrimitiveDef[] primitives; }
    [Serializable] sealed class PrimitiveDef
    {
        public AttributeDef attributes;
        public int indices = -1;
        public int mode = 4;
    }
    [Serializable] sealed class AttributeDef { public int POSITION = -1; public int COLOR_0 = -1; }
    [Serializable] sealed class AccessorDef
    {
        public int bufferView = -1, byteOffset, componentType, count;
        public string type;
        public bool normalized;
    }
    [Serializable] sealed class BufferViewDef { public int byteOffset, byteLength, byteStride; }
}
