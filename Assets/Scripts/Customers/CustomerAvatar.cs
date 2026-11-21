using System.Collections;
using System.Collections.Generic;
using BlackMarket.Art;
using BlackMarket.Audio;
using UnityEngine;

namespace BlackMarket.Customers
{
    public enum AvatarReaction
    {
        Nod,
        Happy,
        Annoyed,
        ShakeHead,
        Thinking
    }

    /// <summary>
    /// A visitor's body in the shop: builds the character from CharacterBuilder and animates it
    /// procedurally (no animation files): walking with swinging limbs, idle breathing, talking,
    /// reactions, and leaning on the counter. Gameplay code tells it where to go and how to react.
    /// </summary>
    public sealed class CustomerAvatar : MonoBehaviour
    {
        private const float WalkSpeed = 1.35f;
        private const float TurnSpeed = 300f;

        private Transform body;
        private Transform head;
        private Transform armLeft;
        private Transform armRight;
        private Transform legLeft;
        private Transform legRight;
        private Transform holdPoint;

        private Vector3 headRest;
        private Vector3 armLeftRest;
        private Vector3 armRightRest;
        private float walkCycle;
        private float walkBlend;
        private float targetYaw;
        private bool walking;
        private float talkUntil;
        private float leanBlend;
        private bool leaning;
        private bool holding;
        private AvatarReaction? reaction;
        private float reactionStart;
        private Transform lookTarget;
        private float seed;

        public bool IsWalking => walking;

        /// <summary>Where a carried item sits, between the hands.</summary>
        public Transform HoldPoint => holdPoint;

        public static CustomerAvatar Create(string lookId, int seed, float browTilt, Vector3 position, float yaw)
        {
            var root = new GameObject("Visitor_" + lookId);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var avatar = root.AddComponent<CustomerAvatar>();
            avatar.Build(lookId, seed, browTilt);
            avatar.targetYaw = yaw;
            return avatar;
        }

        private void Build(string lookId, int buildSeed, float browTilt)
        {
            seed = (buildSeed % 100) * 0.37f;
            CharacterMeshes meshes = CharacterBuilder.Build(lookId, buildSeed, browTilt);
            Material material = ArtLibrary.Current.Character;

            body = ArtLibrary.CreateMeshObject("Body", transform, meshes.Body, material).transform;
            head = Pivot("Head", meshes.HeadPivot, meshes.Head, material);
            armLeft = Pivot("ArmL", meshes.ArmLeftPivot, meshes.ArmLeft, material);
            armRight = Pivot("ArmR", meshes.ArmRightPivot, meshes.ArmRight, material);
            legLeft = Pivot("LegL", meshes.LegLeftPivot, meshes.LegLeft, material);
            legRight = Pivot("LegR", meshes.LegRightPivot, meshes.LegRight, material);
            headRest = head.localPosition;
            armLeftRest = armLeft.localPosition;
            armRightRest = armRight.localPosition;

            holdPoint = new GameObject("HoldPoint").transform;
            holdPoint.SetParent(transform, false);
            holdPoint.localPosition = new Vector3(0f, 1.0f, 0.38f);
        }

        private Transform Pivot(string name, Vector3 at, Mesh mesh, Material material)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = at;
            ArtLibrary.CreateMeshObject("Mesh", pivot, mesh, material);
            return pivot;
        }

        // ------------------------------------------------------------------ Commands

        /// <summary>Walks through the points in order. Yields until arrived.</summary>
        public IEnumerator WalkPath(IList<Vector3> points)
        {
            walking = true;
            foreach (Vector3 target in points)
            {
                while (true)
                {
                    Vector3 to = target - transform.position;
                    to.y = 0f;
                    float dist = to.magnitude;
                    if (dist < 0.03f) break;

                    targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    float step = Mathf.Min(dist, WalkSpeed * Time.deltaTime * Mathf.Lerp(0.4f, 1f, walkBlend));
                    transform.position += to / dist * step;
                    yield return null;
                }
            }

            walking = false;
        }

        public void FaceTowards(Vector3 worldPoint)
        {
            Vector3 to = worldPoint - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 1e-4f) targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        }

        /// <summary>Head gently follows this target (e.g. the player's camera). Null = look ahead.</summary>
        public void LookAt(Transform target) => lookTarget = target;

        public void Talk(float seconds) => talkUntil = Time.time + Mathf.Max(0.3f, seconds);

        public void SetLeaning(bool lean) => leaning = lean;

        /// <summary>Arms held forward, carrying something.</summary>
        public void SetHolding(bool hold) => holding = hold;

        public void React(AvatarReaction r)
        {
            reaction = r;
            reactionStart = Time.time;
        }

        // ------------------------------------------------------------------ Animation

        private void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time + seed;

            // Turn smoothly toward the target direction.
            float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetYaw, TurnSpeed * dt);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            walkBlend = Mathf.MoveTowards(walkBlend, walking ? 1f : 0f, dt * 4f);
            leanBlend = Mathf.MoveTowards(leanBlend, leaning && !walking ? 1f : 0f, dt * 2.5f);
            if (walking)
            {
                float before = Mathf.Sin(walkCycle);
                walkCycle += dt * 8.5f;
                if (Mathf.Sign(before) != Mathf.Sign(Mathf.Sin(walkCycle)))
                {
                    SoundKit.Play(Sound.Footstep, Mathf.Clamp01(2.5f / (1f + transform.position.magnitude)));
                }
            }

            float swing = Mathf.Sin(walkCycle) * 32f * walkBlend;
            float bob = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.045f * walkBlend;
            float breathe = Mathf.Sin(t * 1.7f) * 0.012f * (1f - walkBlend);

            body.localPosition = new Vector3(0f, bob, 0f);
            body.localRotation = Quaternion.Euler(leanBlend * 6f, 0f, Mathf.Sin(walkCycle) * 2.5f * walkBlend);
            body.localScale = new Vector3(1f, 1f + breathe, 1f);

            legLeft.localRotation = Quaternion.Euler(swing, 0f, 0f);
            legRight.localRotation = Quaternion.Euler(-swing, 0f, 0f);

            // Arms: swing while walking, rest on the counter when leaning, held forward when carrying.
            float armForward = Mathf.Lerp(0f, -58f, leanBlend);
            if (holding) armForward = -62f;
            float idleSway = Mathf.Sin(t * 1.3f) * 2f * (1f - walkBlend);
            armLeft.localRotation = Quaternion.Euler(-swing * 0.8f * (holding ? 0f : 1f) + armForward + idleSway, 0f, holding ? 12f : -6f);
            armRight.localRotation = Quaternion.Euler(swing * 0.8f * (holding ? 0f : 1f) + armForward - idleSway, 0f, holding ? -12f : 6f);
            armLeft.localPosition = armLeftRest + new Vector3(0f, bob + breathe, 0f);
            armRight.localPosition = armRightRest + new Vector3(0f, bob + breathe, 0f);

            AnimateHead(t, bob);
        }

        private void AnimateHead(float t, float bob)
        {
            float pitch = 0f, yaw = 0f, roll = 0f;
            Vector3 offset = Vector3.zero;

            // Look at the player when standing still.
            if (lookTarget != null && !walking)
            {
                Vector3 local = transform.InverseTransformPoint(lookTarget.position) - head.localPosition;
                yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -35f, 35f);
                pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -20f, 20f);
            }

            // Idle drift
            yaw += Mathf.Sin(t * 0.6f) * 4f;
            pitch += Mathf.Sin(t * 0.9f) * 2f;

            if (Time.time < talkUntil)
            {
                pitch += Mathf.Sin(t * 11f) * 3.5f;
                roll += Mathf.Sin(t * 4.3f) * 2.5f;
            }

            if (reaction.HasValue)
            {
                float rt = Time.time - reactionStart;
                switch (reaction.Value)
                {
                    case AvatarReaction.Nod:
                        pitch += Mathf.Sin(rt * 14f) * 12f * Mathf.Clamp01(1f - rt / 0.8f);
                        break;
                    case AvatarReaction.Happy:
                        offset.y += Mathf.Abs(Mathf.Sin(rt * 9f)) * 0.06f * Mathf.Clamp01(1f - rt / 1.1f);
                        roll += Mathf.Sin(rt * 9f) * 6f * Mathf.Clamp01(1f - rt / 1.1f);
                        break;
                    case AvatarReaction.Annoyed:
                        pitch += 10f * Mathf.Clamp01(1f - rt / 1.2f);
                        roll += Mathf.Sin(rt * 30f) * 2f * Mathf.Clamp01(1f - rt / 0.6f);
                        break;
                    case AvatarReaction.ShakeHead:
                        yaw += Mathf.Sin(rt * 16f) * 18f * Mathf.Clamp01(1f - rt / 0.9f);
                        break;
                    case AvatarReaction.Thinking:
                        roll += 12f * Mathf.Clamp01(Mathf.Min(rt * 4f, (1.6f - rt) * 4f));
                        pitch -= 6f * Mathf.Clamp01(Mathf.Min(rt * 4f, (1.6f - rt) * 4f));
                        break;
                }

                if (rt > 1.6f) reaction = null;
            }

            head.localPosition = headRest + new Vector3(0f, bob, 0f) + offset;
            head.localRotation = Quaternion.Euler(pitch + leanBlend * 4f, yaw, roll);
        }
    }
}
