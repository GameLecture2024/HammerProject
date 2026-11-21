using UnityEngine;
using UnityEngine.InputSystem;

namespace ReinforceGame
{
    public sealed class FirstPersonRig : MonoBehaviour
    {
        public Camera view;
        public Transform rightUpper, rightLower, rightHand, leftUpper, leftLower, leftHand;
        public Transform hammerGrip, itemGrip;
        public Renderer leftArm;
        public bool carrying;
        public float sensitivity = .13f;
        public float strike;
        InputAction look, rotate;
        float yaw, pitch;

        void OnEnable()
        {
            look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            rotate = new InputAction("Rotate", InputActionType.Button, "<Mouse>/rightButton");
            look.Enable(); rotate.Enable();
        }
        void OnDisable()
        {
            look?.Dispose(); rotate?.Dispose();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        void Update()
        {
            bool turning = rotate.IsPressed();
            Cursor.lockState = turning ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !turning;
            if (!turning) return;
            var delta = look.ReadValue<Vector2>();
            yaw = Mathf.Clamp(yaw + delta.x * sensitivity, -105, 105);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -45, 32);
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
        }
        void LateUpdate()
        {
            if (leftArm) leftArm.gameObject.layer = carrying ? 30 : 31;
            Vector3 hammerTarget = transform.TransformPoint(new Vector3(.32f, -.32f + strike * .32f, .58f - strike * .12f));
            Vector3 itemTarget = transform.TransformPoint(new Vector3(-.32f, -.35f, .65f));
            Solve(rightUpper, rightLower, rightHand, hammerTarget, -transform.up + transform.right);
            Solve(leftUpper, leftLower, leftHand, itemTarget, -transform.up - transform.right);
            if (rightHand) rightHand.rotation = transform.rotation * Quaternion.Euler(-20 + strike * -65, 0, 0);
            if (leftHand) leftHand.rotation = transform.rotation * Quaternion.Euler(0, 0, -20);
        }
        public static void Solve(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 bend)
        {
            if (!upper || !lower || !hand) return;
            var origin = upper.position;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, hand.position);
            var direction = target - origin;
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
            var axis = direction.normalized;
            var perpendicular = Vector3.ProjectOnPlane(bend, axis).normalized;
            float along = (a * a + distance * distance - b * b) / (2 * distance);
            var elbow = origin + axis * along + perpendicular * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, elbow - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
        }
    }
}
