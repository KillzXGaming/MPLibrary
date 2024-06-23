using IONET;
using IONET.Core;
using IONET.Core.Animation;
using IONET.Core.Model;
using MPLibrary.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.Animations;

namespace MPLibrary.GCWii.HSF
{
    public class HSFAnimationExporter
    {
        public static void Export(HsfFile hsfFile, HSFMotionAnimation anim, string filePath)
        {
            IOScene scene = new IOScene();
            IOModel model = new IOModel();
            scene.Models.Add(model);

            //Bones
            List<IONode> bones = new List<IONode>();
            foreach (var ob in hsfFile.ObjectNodes)
            {
                var info = ob.Data;

                //Add a dummy bone. Some bone data is set at runtime and uses large random values
                var bone = new IONode()
                {
                    Name = ob.Name,
                    RotationEuler = System.Numerics.Vector3.Zero,
                    Translation = System.Numerics.Vector3.Zero,
                    Scale = System.Numerics.Vector3.One,
                };
                if (ob.Data.ChildrenCount <= hsfFile.ObjectNodes.Count)
                {
                    bone = new IONode()
                    {
                        Name = ob.Name,
                        Translation = new System.Numerics.Vector3(
                          info.BaseTransform.Translate.X,
                          info.BaseTransform.Translate.Y,
                          info.BaseTransform.Translate.Z),
                        RotationEuler = new System.Numerics.Vector3(
                      OpenTK.MathHelper.DegreesToRadians(info.BaseTransform.Rotate.X),
                      OpenTK.MathHelper.DegreesToRadians(info.BaseTransform.Rotate.Y),
                      OpenTK.MathHelper.DegreesToRadians(info.BaseTransform.Rotate.Z)),
                        Scale = new System.Numerics.Vector3(
                          info.BaseTransform.Scale.X == 0 ? 1 : info.BaseTransform.Scale.X,
                          info.BaseTransform.Scale.Y == 0 ? 1 : info.BaseTransform.Scale.Y,
                          info.BaseTransform.Scale.Z == 0 ? 1 : info.BaseTransform.Scale.Z),
                    };
                }
                bone.IsJoint = true;

                bones.Add(bone);
            }

            for (int i = 0; i < hsfFile.ObjectNodes.Count; i++)
            {
                var parentIndex = hsfFile.ObjectNodes.IndexOf(hsfFile.ObjectNodes[i].Parent);

                if (parentIndex == -1)
                    model.Skeleton.RootBones.Add(bones[i]);
                else
                    bones[parentIndex].AddChild(bones[i]);
            }
            scene.Nodes.AddRange(bones);

            //Animation data
            IOAnimation ioanim = new IOAnimation() { Name = anim.Name };
            scene.Animations.Add(ioanim);   

            foreach (AnimationNode animGroup in anim.AnimGroups)
            {
                if (animGroup.Mode == TrackMode.Normal)
                {
                    IOAnimation iogroup = new IOAnimation() { Name = animGroup.Name };
                    ioanim.Groups.Add(iogroup);

                    void AddTrack(AnimTrack track, IOAnimationTrackType type)
                    {
                        IOAnimationTrack iotrack = new IOAnimationTrack() {  ChannelType = type, };
                        iogroup.Tracks.Add(iotrack);

                        bool isRotation = type.ToString().StartsWith("RotationEuler");
                        float ConvertValue(float v)
                        {
                            if (isRotation) return v * STMath.Deg2Rad;

                            return v;
                        }

                        if (track.KeyFrames.Count == 0)
                        {
                            iotrack.KeyFrames.Add(new IOKeyFrame()
                            {
                                Frame = 0,
                                Value = ConvertValue(track.Constant),
                            });
                            return;
                        }

                        foreach (var keyframe in track.KeyFrames) {

                            if (keyframe is STBezierKeyFrame)
                            {
                                iotrack.KeyFrames.Add(new IOKeyFrameHermite()
                                {
                                    Frame = keyframe.Frame,
                                    Value = ConvertValue(keyframe.Value),
                                    TangentSlopeInput = ConvertValue(((STBezierKeyFrame)keyframe).SlopeIn),
                                    TangentSlopeOutput = ConvertValue(((STBezierKeyFrame)keyframe).SlopeOut),
                                });
                            }
                            else
                            {
                                iotrack.KeyFrames.Add(new IOKeyFrame()
                                {
                                    Frame = keyframe.Frame,
                                    Value = ConvertValue(keyframe.Value),
                                });
                            }
                        }
                    }

                    foreach (var track in animGroup.TrackList)
                    {
                        switch (track.TrackEffect)
                        {
                            case TrackEffect.TranslateX: AddTrack(track, IOAnimationTrackType.PositionX); break;
                            case TrackEffect.TranslateY: AddTrack(track, IOAnimationTrackType.PositionY); break;
                            case TrackEffect.TranslateZ: AddTrack(track, IOAnimationTrackType.PositionZ); break;
                            case TrackEffect.RotationX: AddTrack(track, IOAnimationTrackType.RotationEulerX); break;
                            case TrackEffect.RotationY: AddTrack(track, IOAnimationTrackType.RotationEulerY); break;
                            case TrackEffect.RotationZ: AddTrack(track, IOAnimationTrackType.RotationEulerZ); break;
                            case TrackEffect.ScaleX: AddTrack(track, IOAnimationTrackType.ScaleX); break;
                            case TrackEffect.ScaleY: AddTrack(track, IOAnimationTrackType.ScaleY); break;
                            case TrackEffect.ScaleZ: AddTrack(track, IOAnimationTrackType.ScaleZ); break;
                        }
                    }
                }
            }

            IOManager.ExportScene(scene, filePath, new ExportSettings()
            {
                ExportAnimations = true,
                MayaAnim2015 = true,
                MayaAnimUseRadians = false,
            });
        }
    }
}
