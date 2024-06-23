using MPLibrary.GCN;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MPLibrary.GCWii.HSF
{
    public class HSFJsonExporter
    {
        public List<ObjectNode> Nodes = new List<ObjectNode>();
        public List<EnvelopeData> Envelopes = new List<EnvelopeData>();

        public static void Export(HsfFile hsf, string filePath)
        {
            HSFJsonExporter exporter = new HSFJsonExporter();
            File.WriteAllText(filePath, exporter.ToText(hsf));
        }

        public string ToText(HsfFile hsf)
        {
            foreach (var n in hsf.ObjectNodes)
            {
                Nodes.Add(new ObjectNode()
                {
                    Name = n.Name,
                    ObjectData = n.Data,
                });
            }
            foreach (var mesh in hsf.Meshes)
            {
                if (mesh.HasEnvelopes)
                {
                    var env = new EnvelopeData();
                    var cenv = mesh.Envelopes.FirstOrDefault();

                    for (int i = 0; i < cenv.SingleBinds.Count; i++)
                    {
                        env.SingleRigs.Add(new SingleRig()
                        {
                            BoneIndex = cenv.SingleBinds[i].BoneIndex,
                            PositionCount = cenv.SingleBinds[i].PositionCount,
                            PositionIndex = cenv.SingleBinds[i].PositionIndex,
                            NormalCount = cenv.SingleBinds[i].NormalCount,
                            NormalIndex = cenv.SingleBinds[i].NormalIndex,
                        });
                    }

                    for (int i = 0; i < cenv.SingleBinds.Count; i++)
                    {
                        env.SingleRigs.Add(new SingleRig()
                        {
                            BoneIndex = cenv.SingleBinds[i].BoneIndex,
                            PositionCount = cenv.SingleBinds[i].PositionCount,
                            PositionIndex = cenv.SingleBinds[i].PositionIndex,
                            NormalCount = cenv.SingleBinds[i].NormalCount,
                            NormalIndex = cenv.SingleBinds[i].NormalIndex,
                        });
                    }

                    int double_bind_index = 0;
                    for (int i = 0; i < cenv.DoubleBinds.Count; i++)
                    {
                        var db = new DoubleRig();
                        db.Bone1 = cenv.DoubleBinds[i].Bone1;
                        db.Bone2 = cenv.DoubleBinds[i].Bone2;
                        env.DoubleRigs.Add(db);

                        for (int j = 0; j < cenv.DoubleBinds[i].Count; j++)
                        {
                            var double_weight = cenv.DoubleWeights[double_bind_index];

                            db.Weights.Add(new DoubleRigWeight()
                            {
                                Weight = double_weight.Weight,
                                PositionCount = double_weight.PositionCount,
                                PositionIndex = double_weight.PositionIndex,
                                NormalCount = double_weight.NormalCount,
                                NormalIndex = double_weight.NormalIndex,
                            });
                            double_bind_index++;
                        }
                    }

                    Envelopes.Add(env);
                }
            }
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public class ObjectNode
        {
            public string Name { get; set; }

            public HSFObjectData ObjectData;
        }

        public class EnvelopeData
        {
            public List<SingleRig> SingleRigs = new List<SingleRig>();
            public List<DoubleRig> DoubleRigs = new List<DoubleRig>();
            public List<MultiRig> MultiRigs = new List<MultiRig>();
        }

        public class SingleRig
        {
            public int BoneIndex;

            [JsonIgnore]
            public short PositionIndex;
            [JsonIgnore]
            public short PositionCount;
            [JsonIgnore]
            public short NormalIndex;
            [JsonIgnore]
            public short NormalCount;
        }

        public class DoubleRig
        {
            public int Bone1;
            public int Bone2;
            public List<DoubleRigWeight> Weights = new List<DoubleRigWeight>();
        }

        public class DoubleRigWeight
        {
            public float Weight;
            [JsonIgnore]
            public short PositionIndex;
            [JsonIgnore]
            public short PositionCount;
            [JsonIgnore]
            public short NormalIndex;
            [JsonIgnore]
            public short NormalCount;
        }

        public class MultiRig
        {
            [JsonIgnore]
            public short PositionIndex;
            [JsonIgnore]
            public short PositionCount;
            [JsonIgnore]
            public short NormalIndex;
            [JsonIgnore]
            public short NormalCount;

            public List<MultiRigWeight> Weights = new List<MultiRigWeight>();
        }

        public class MultiRigWeight
        {
            public int Bone;
            public float Weight;
        }
    }
}
