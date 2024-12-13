using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class HandModelTracker : MonoBehaviour
{
    public GameObject leftHandRoot; // Root GameObject of your hand model
    public GameObject thumbTipBone;
    public GameObject indexTipBone;
    public GameObject middleTipBone;
    public GameObject ringTipBone;
    public GameObject pinkyTipBone;

    public Vector3 wristOffsetRotation = new Vector3(0, 0, 0); // Adjust this as needed

    void Update()
    {
        // Track the wrist joint
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Left, out MixedRealityPose wristPose))
        {
            leftHandRoot.transform.position = wristPose.Position;

            // Apply orientation offset
            Quaternion offset = Quaternion.Euler(wristOffsetRotation);
            leftHandRoot.transform.rotation = wristPose.Rotation * offset;
        }

        // Track each finger tip
        UpdateFingerJoint(TrackedHandJoint.ThumbTip, thumbTipBone);
        UpdateFingerJoint(TrackedHandJoint.IndexTip, indexTipBone);
        UpdateFingerJoint(TrackedHandJoint.MiddleTip, middleTipBone);
        UpdateFingerJoint(TrackedHandJoint.RingTip, ringTipBone);
        UpdateFingerJoint(TrackedHandJoint.PinkyTip, pinkyTipBone);
    }

    private void UpdateFingerJoint(TrackedHandJoint joint, GameObject fingerBone)
    {
        if (fingerBone != null && HandJointUtils.TryGetJointPose(joint, Handedness.Left, out MixedRealityPose jointPose))
        {
            fingerBone.transform.position = jointPose.Position;
            fingerBone.transform.rotation = jointPose.Rotation;
        }
    }
}
