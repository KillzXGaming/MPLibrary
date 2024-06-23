using IONET.Core.Model;
using IONET.Core;
using MPLibrary.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace MPLibrary.GCWii.HSF
{
    public class HSFModelImporterEnvelopeCreator
    {
        public List<Vector3> position_list = new List<Vector3>();
        public List<Vector3> normals_list = new List<Vector3>();

        public Dictionary<int, VertexGroup> pos_nrm_info = new Dictionary<int, VertexGroup>();

        public HSFEnvelope GenerateEnvelopeData(HsfFile hsf, IOMesh mesh)
        {
            position_list.Clear();
            normals_list.Clear();
            pos_nrm_info.Clear();

            return ComputeEnvelopeData(hsf, mesh);
        }

        private HSFEnvelope ComputeEnvelopeData(HsfFile hsf, IOMesh mesh)
        {
            List<SingleBind> singleBinds = new List<SingleBind>();
            List<DoubleBind> doubleBinds = new List<DoubleBind>();
            List<MultiBind> multiBinds = new List<MultiBind>();

            List<IOVertex> vertices = mesh.Vertices;

            List<string> missing_bones = new List<string>();

            //With all the vertices sorted, add each vertex in order of similar rigging info
            for (int v = 0; v < vertices.Count; v++)
            {
                var vertex = vertices[v];
                var vertex_id = v;

                int[] bone_indices = new int[4];
                float[] bone_weights = new float[4];

                Vector3 position = vertex.Position;
                Vector3 normal = vertex.Normal;

                vertex.Envelope.Normalize();

                var envelopes = vertex.Envelope.Weights.OrderBy(x => x.BoneName).Where(x => x.Weight != 0).ToList();

                for (int j = 0; j < envelopes.Count; j++)
                {
                    //Round weight precision to decrease file size
                    var bone_name = envelopes[j].BoneName;
                    var bone_weight = envelopes[j].Weight;

                    //Check for duplicate bones via 3ds max and remove the dupe name
                    //This is useful for when there is a custom boneset renamed, then the file has a duped boneset using original parts
                    if (bone_name.Contains("_ncl1_"))
                        bone_name = bone_name.Split("_ncl1_").FirstOrDefault();

                    //The searched bone id
                    var bone_idx = hsf.ObjectNodes.FindIndex(x => x.Name == bone_name);
                    if (bone_idx == -1) //failed to find bone
                    {
                        //Fall back to certain bones when not present
                        //This is for LODs that have less bones
                        if (bone_name == "ske_hair3")
                            bone_idx = hsf.ObjectNodes.FindIndex(x => x.Name == "ske_hair2");  //force rig to ske_hair2
                        if (bone_name.StartsWith("ske_L_fing"))
                            bone_idx = hsf.ObjectNodes.FindIndex(x => x.Name == "ske_L_hand");
                        if (bone_name.StartsWith("ske_R_fing"))
                            bone_idx = hsf.ObjectNodes.FindIndex(x => x.Name == "ske_R_hand");
                    }
                    if (bone_idx == -1) //failed to find bone
                    {
                        bone_idx = 0;

                        if (!missing_bones.Contains(bone_name))
                            missing_bones.Add(bone_name);
                    }
                    bone_indices[j] = bone_idx;
                    bone_weights[j] = bone_weight;
                }

              //  Console.WriteLine($"bone_idx {string.Join(",", bone_indices)} bone_weights {string.Join(",", bone_weights)}");

                switch (envelopes.Count)
                {
                    case 0: //no rigging, just force to rig first bone
                    case 1:
                        //Search if the bind info exists
                        var singe_bind = singleBinds.FirstOrDefault(x => x.BoneIndex == bone_indices[0]);
                        if (singe_bind == null) //Add bone index if does not exist
                        {
                            singe_bind = new SingleBind() { BoneIndex = (int)bone_indices[0], };
                            singleBinds.Add(singe_bind);
                        }
                        //Add position and normals to the buffer for calculating at runtime
                        singe_bind.Vertices.Add(new VertexTarget()
                        {
                            Position = position,
                            Normal = normal,
                            VertexID = vertex_id,
                        });
                        break;
                    case 2: //double binds
                        var bone_idx_0 = (int)bone_indices[0];
                        var bone_idx_1 = (int)bone_indices[1];
                        //weights to use. Only use the first weight, second is normalized to 1.0
                        var bone_weight = bone_weights[0];

                        //Search if the bind info exists
                        var double_bind = doubleBinds.FirstOrDefault(x => 
                            x.Bone1 == bone_idx_0 && x.Bone2 == bone_idx_1);
                        if (double_bind == null) //bone is not present, add it
                        {
                            double_bind = new DoubleBind() { Bone1 = bone_idx_0, Bone2 = bone_idx_1, };
                            doubleBinds.Add(double_bind);
                        }

                        //Search if the target weight exists
                        var double_bind_weight = double_bind.Weights.FirstOrDefault(x => x.Weight == bone_weight);
                        if (double_bind_weight == null) //weight is not present, add it
                        {
                            double_bind_weight = new DoubleBindWeight() { Weight = bone_weight};
                            double_bind.Weights.Add(double_bind_weight);
                        }

                        //Add position and normals to the buffer for calculating at runtime
                        double_bind_weight.Vertices.Add(new VertexTarget()
                        {
                            Position = position,
                            Normal = normal,
                            VertexID = vertex_id,
                        });
                        break;
                    case 3:
                    case 4: //multi binds
                    default:
                        MultiBind bind = new MultiBind();
                        for (int j = 0; j < envelopes.Count; j++)
                        {
                            bind.Weights.Add(new MultiBindWeight()
                            {
                                BoneIndex = bone_indices[j],
                                Weight = bone_weights[j],
                            });
                        }

                        //Search if the bind info exists
                        var multi_bind = multiBinds.FirstOrDefault(x => x.IsMatch(bind));
                        if (multi_bind == null)
                        {
                            multiBinds.Add(bind);
                            multi_bind = bind;
                        }

                        //Add position and normals to the buffer for calculating at runtime
                        multi_bind.Vertices.Add(new VertexTarget()
                        {
                            Position = position,
                            Normal = normal,
                            VertexID = vertex_id,
                        });
                        break;
                }
            }

            StudioLogger.ResetErrors();
            foreach (var bone in missing_bones)
            {
                StudioLogger.WriteWarning($"Failed to find bone {bone} in HSF node tree! Make sure you use the original HSF bones.");
                Console.WriteLine($"Failed to find bone {bone} in HSF node tree! Make sure you use the original HSF bones.");
            }

            //Order by bone ID to better match the generated binaries
            singleBinds = singleBinds.OrderBy(x => x.BoneIndex).ToList();
            doubleBinds = doubleBinds.OrderBy(x => x.Bone1).ThenBy(x => x.Bone2).ToList();

            return ProcessEnvelopeData(singleBinds, doubleBinds, multiBinds);
        }

        private HSFEnvelope ProcessEnvelopeData(List<SingleBind> singleBinds,
             List<DoubleBind> doubleBinds, List<MultiBind> multiBinds)
        {
            //Process the current data into expected HSF format
            //The game expects to modify all the vertex positions and normals in a buffer using indices and counters
            HSFEnvelope riggingInfo = new HSFEnvelope();

            //Adds the batch of vertices that share the same rigging info
            void AddVerticesBatch(List<VertexTarget> vertices, ref short pos_count, ref short nrm_count)
            {
                //Condense the list of all duplicate vertex positions or normals
                //Todo properly remove dupes
                var p_list = new List<Vector3>();
                var n_list = new List<Vector3>();

                foreach (var v in vertices)
                {
                    if (!p_list.Contains(v.Position)) p_list.Add(v.Position);
                    if (!n_list.Contains(v.Normal))   n_list.Add(v.Normal);
                }

                //Shift global buffer index
                var global_pos_index = (short)position_list.Count;
                var global_nrm_index = (short)normals_list.Count;

                for (int i = 0; i < vertices.Count; i++)
                {
                    //index in the envelope batch
                    int posIdx = p_list.IndexOf(vertices[i].Position);
                    int nrmIdx = n_list.IndexOf(vertices[i].Normal);
                    //Add to lookup
                    //This gets checked on which position and normal index to reference
                    //when the vertex gets added to the primitive list
                    pos_nrm_info.Add(vertices[i].VertexID, new VertexGroup()
                    {
                        PositionIndex = (short)(global_pos_index + posIdx),
                        NormalIndex = (short)(global_nrm_index + nrmIdx),
                    });
                }

                //Add to buffer data
                position_list.AddRange(p_list);
                normals_list.AddRange(n_list);


                pos_count = (short)p_list.Count;
                nrm_count = (short)n_list.Count;
            }

            foreach (var bind in singleBinds)
            {
                short pos_idx = (short)position_list.Count;
                short nrm_idx = (short)normals_list.Count;
                short pos_count = 0;
                short nrm_count = 0;

                AddVerticesBatch(bind.Vertices, ref pos_count, ref nrm_count);

                riggingInfo.SingleBinds.Add(new RiggingSingleBind()
                {
                    BoneIndex = bind.BoneIndex,
                    PositionIndex = pos_idx,
                    NormalIndex = nrm_idx,
                    PositionCount = pos_count,
                    NormalCount = nrm_count,
                });
            }
            foreach (var bind in doubleBinds)
            {
                var db = new RiggingDoubleBind() { Bone1 = bind.Bone1, Bone2 = bind.Bone2, Count = bind.Weights.Count, };
                riggingInfo.DoubleBinds.Add(db);

                foreach (var bind_weight in bind.Weights)
                {
                    short pos_idx = (short)position_list.Count;
                    short nrm_idx = (short)normals_list.Count;
                    short pos_count = 0;
                    short nrm_count = 0;

                    AddVerticesBatch(bind_weight.Vertices, ref pos_count, ref nrm_count);

                    riggingInfo.DoubleWeights.Add(new RiggingDoubleWeight()
                    {
                        Weight = bind_weight.Weight,
                        PositionIndex = pos_idx,
                        NormalIndex = nrm_idx,
                        PositionCount = pos_count,
                        NormalCount = nrm_count,
                    });
                }
            }
            foreach (var bind in multiBinds)
            {
                short pos_idx = (short)position_list.Count;
                short nrm_idx = (short)normals_list.Count;
                short pos_count = 0;
                short nrm_count = 0;

                AddVerticesBatch(bind.Vertices, ref pos_count, ref nrm_count);

                riggingInfo.MultiBinds.Add(new RiggingMultiBind()
                {
                    Count = bind.Weights.Count,
                    PositionIndex = pos_idx,
                    NormalIndex = nrm_idx,
                    PositionCount = pos_count,
                    NormalCount = nrm_count,
                });

                foreach (var bind_weight in bind.Weights)
                {
                    riggingInfo.MultiWeights.Add(new RiggingMultiWeight()
                    {
                        Weight = bind_weight.Weight,
                        BoneIndex = bind_weight.BoneIndex,
                    });
                }
            }

            riggingInfo.VertexCount = (uint)position_list.Count;
            riggingInfo.CopyCount = 0;

            return riggingInfo;
        }

        class SingleBind
        {
            public int BoneIndex;

            public List<VertexTarget> Vertices = new List<VertexTarget>();
        }

        class DoubleBind
        {
            public int Bone1;
            public int Bone2;

            public List<DoubleBindWeight> Weights = new List<DoubleBindWeight>();
        }

        class DoubleBindWeight
        {
            public float Weight;

            public List<VertexTarget> Vertices = new List<VertexTarget>();
        }

        class MultiBind
        {
            public List<VertexTarget> Vertices = new List<VertexTarget>();

            public List<MultiBindWeight> Weights = new List<MultiBindWeight>();

            public bool IsMatch(MultiBind bind)
            {
                for (int i = 0; i < Weights.Count; i++)
                {
                    if (bind.Weights[i].Weight != Weights[i].Weight ||
                        bind.Weights[i].BoneIndex != Weights[i].BoneIndex)
                    {
                        return false;
                    }
                }
                return false; //seems they don't optimize duped multi binds, so return false for now

                return true;
            }
        }

        class MultiBindWeight
        {
            public int BoneIndex;
            public float Weight;
        }

        class VertexTarget
        {
            public Vector3 Position;
            public Vector3 Normal;
            public int VertexID;
        }
    }
}
