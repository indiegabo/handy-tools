using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.Platformer2D
{
    [AddComponentMenu("HandyTools/Platformer/Flip2D")]
    public class Flip2D : HandyBehaviour
    {
        [BoxGroup("Dependencies")]
        [Tooltip("The subject wich will be flipped. If null, the object's transform will be used")]
        [SerializeField]
        private Transform _subject;

        [BoxGroup("Configuration")]
        [BoxGroup("Configuration/Strategy")]
        [Tooltip("If the game object should be flipped scaling, rotating or using the sprite renderer")]
        [LabelText("Strategy")]
        [EnumToggleButtons]
        [SerializeField]
        private FlipStrategy _strategy = FlipStrategy.Rotating;

        [BoxGroup("Configuration/Strategy")]
        [ShowIf("UsesSpriteStrategy")]
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [BoxGroup("Configuration/Directions")]
        [Tooltip("Use this to set wich direction GameObject should start flipped towards.")]
        [LabelText("Starting Horizontal")]
        [EnumToggleButtons]
        [SerializeField]
        private HorizontalDirection _horizontalStartingDirection = HorizontalDirection.Right;

        [BoxGroup("Configuration/Directions")]
        [Tooltip("Use this to set wich direction GameObject should start flipped towards.")]
        [LabelText("Starting Vertical")]
        [EnumToggleButtons]
        [SerializeField]
        private VerticalDirection _verticalStartingDirection = VerticalDirection.Down;

        // Events
        [FoldoutGroup("Events")]
        [SerializeField]
        private UnityEvent<int> _facingDirectionSignUpdate;

        [FoldoutGroup("Events")]
        [SerializeField]
        private UnityEvent<int> _verticalDirectionSignUpdate;

        #region Fields

        private bool _locked = false;
        private Transform FlipSubject => _subject ? _subject : transform;
        private int _currentHorizontalDirectionSign;

        private int _currentVerticalDirectionSign;
        #endregion

        #region Properties

        /// <summary>
        /// The current horizontal direction sign (either -1 or 1)
        /// </summary>
        public int CurrentHorizontalDirectionSign => _currentHorizontalDirectionSign;

        /// <summary>
        /// The current vertical direction sign (either -1 or 1)
        /// </summary>
        public int CurrentVerticalDirectionSign => _currentVerticalDirectionSign;

        #endregion

        #region Getters

        public bool UsesScalingStrategy => _strategy == FlipStrategy.Scaling;
        public bool UsesRotatingStrategy => _strategy == FlipStrategy.Rotating;
        public bool UsesSpriteStrategy => _strategy == FlipStrategy.Sprite;

        public UnityEvent<int> FacingDirectionSignUpdate => _facingDirectionSignUpdate;
        public UnityEvent<int> VerticalDirectionSignUpdate => _verticalDirectionSignUpdate;

        public bool Locked => _locked;

        #endregion

        #region Mono

        protected virtual void Start()
        {
            InitialHorizontalFlip();
            InitialVerticalFlip();
        }

        #endregion

        #region Logic

        /// <summary>
        /// Evaluates if the game object can be flipped based on subjectDirection and if so, performs it.
        /// </summary>
        /// <param name="subjectDirection"></param>
        public virtual void EvaluateAndFlipHorizontally(float subjectDirection)
        {
            if (subjectDirection == 0) return;
            if (!ShouldFlipHorizontally(subjectDirection > 0 ? 1 : -1)) return;
            FlipHorizontally();
        }

        /// <summary>
        /// Evaluates if the game object can be flipped based on subjectDirection and if so, performs it.
        /// </summary>
        /// <param name="subjectDirection"></param>
        public virtual void EvaluateAndFlipHorizontally(int subjectDirection)
        {
            if (subjectDirection == 0) return;
            if (!ShouldFlipHorizontally(subjectDirection > 0 ? 1 : -1)) return;
            FlipHorizontally();
        }


        /// <summary>
        /// Evaluates if the game object can be flipped based on subjectDirection and if so, performs it.
        /// </summary>
        /// <param name="subjectDirection"></param>
        public virtual void EvaluateAndFlipVertically(float subjectDirection)
        {
            if (subjectDirection == 0) return;
            if (!ShouldFlipVertically(subjectDirection > 0 ? 1 : -1)) return;
            FlipVertically();
        }

        /// <summary>
        /// Evaluates if the game object can be flipped based on subjectDirection and if so, performs it.
        /// </summary>
        /// <param name="subjectDirection"></param>
        public virtual void EvaluateAndFlipVertically(int subjectDirection)
        {
            if (subjectDirection == 0) return;
            if (!ShouldFlipVertically(subjectDirection > 0 ? 1 : -1)) return;
            FlipVertically();
        }

        /// <summary>
        /// Flips character horizontally based on current horizontal flip strategy
        /// and current horizontal direction.
        /// </summary>
        public virtual void FlipHorizontally()
        {
            UpdateHorizontalDirection(_currentHorizontalDirectionSign * -1);

            switch (_strategy)
            {
                case FlipStrategy.Rotating:
                    FlipSubject.Rotate(0f, -180f, 0f);
                    break;
                case FlipStrategy.Scaling:
                    FlipSubject.localScale = new Vector3(FlipSubject.localScale.x * -1, FlipSubject.localScale.y, FlipSubject.localScale.z);
                    break;
                case FlipStrategy.Sprite:
                    _spriteRenderer.flipX = !_spriteRenderer.flipX;
                    break;
            }
        }

        /// <summary>
        /// Flips character vertically based on current vertical flip strategy
        /// and current vertical direction.
        /// </summary>
        public virtual void FlipVertically()
        {
            UpdateVerticalDirection(_currentVerticalDirectionSign * -1);

            switch (_strategy)
            {
                case FlipStrategy.Rotating:
                    FlipSubject.Rotate(-180f, 0f, 0f);
                    break;
                case FlipStrategy.Scaling:
                    FlipSubject.localScale = new Vector3(FlipSubject.localScale.x, FlipSubject.localScale.y * -1, FlipSubject.localScale.z);
                    break;
                default:
                    FlipSubject.Rotate(-180f, 0f, 0f);
                    break;
                case FlipStrategy.Sprite:
                    _spriteRenderer.flipY = !_spriteRenderer.flipY;
                    break;
            }
        }

        protected virtual void UpdateHorizontalDirection(int directionSign)
        {
            _currentHorizontalDirectionSign = directionSign;
            FacingDirectionSignUpdate.Invoke(_currentHorizontalDirectionSign);
        }

        protected virtual void UpdateVerticalDirection(int directionSign)
        {
            _currentVerticalDirectionSign = directionSign;
            VerticalDirectionSignUpdate.Invoke(_currentVerticalDirectionSign);
        }

        /// <summary>
        /// Executes an initial Flip of the GameObject
        /// based on the startingDirection chosen on
        /// inspector or the current <see cref="Flip2D"/> status.
        /// </summary>
        protected virtual void InitialHorizontalFlip()
        {
            if (_horizontalStartingDirection == HorizontalDirection.Right)
            {
                switch (_strategy)
                {
                    case FlipStrategy.Rotating:
                        if (FlipSubject.rotation.y != 0f)
                            FlipSubject.rotation = Quaternion.Euler(FlipSubject.rotation.eulerAngles.x, 0f, 0f);
                        break;
                    case FlipStrategy.Scaling:
                        if (FlipSubject.localScale.x < 0)
                            FlipSubject.localScale = new Vector3(FlipSubject.localScale.x * -1, FlipSubject.localScale.y, FlipSubject.localScale.z);
                        break;
                    case FlipStrategy.Sprite:
                        _spriteRenderer.flipX = false;
                        break;
                }
                UpdateHorizontalDirection(1);
                return;
            }

            if (_horizontalStartingDirection == HorizontalDirection.Left)
            {
                switch (_strategy)
                {
                    case FlipStrategy.Rotating:
                        if (Mathf.Abs(FlipSubject.rotation.y) != 180f)
                            FlipSubject.rotation = Quaternion.Euler(FlipSubject.rotation.eulerAngles.x, 180f, 0f);
                        break;
                    case FlipStrategy.Scaling:
                        if (FlipSubject.localScale.x > 0)
                            FlipSubject.localScale = new Vector3(FlipSubject.localScale.x * -1, FlipSubject.localScale.y, FlipSubject.localScale.z);
                        break;
                    case FlipStrategy.Sprite:
                        _spriteRenderer.flipX = true;
                        break;
                }
                UpdateHorizontalDirection(-1);
                return;
            }
        }

        /// <summary>
        /// Executes an initial Flip of the GameObject
        /// based on the startingDirection chosen on
        /// inspector or the current <see cref="Flip2D"/> status.
        /// </summary>
        protected virtual void InitialVerticalFlip()
        {
            if (_verticalStartingDirection == VerticalDirection.Down)
            {
                switch (_strategy)
                {
                    case FlipStrategy.Rotating:
                        if (FlipSubject.rotation.x != 0f)
                            FlipSubject.rotation = Quaternion.Euler(0f, FlipSubject.rotation.eulerAngles.y, 0f);
                        break;
                    case FlipStrategy.Scaling:
                        if (FlipSubject.localScale.y < 0)
                            FlipSubject.localScale = new Vector2(FlipSubject.localScale.x, FlipSubject.localScale.y * -1);
                        break;
                    case FlipStrategy.Sprite:
                        _spriteRenderer.flipY = false;
                        break;
                }
                UpdateVerticalDirection(-1);
                return;
            }

            if (_verticalStartingDirection == VerticalDirection.Up)
            {
                switch (_strategy)
                {
                    case FlipStrategy.Rotating:
                        if (Mathf.Abs(FlipSubject.rotation.x) != 180f)
                            FlipSubject.rotation = Quaternion.Euler(180f, FlipSubject.rotation.eulerAngles.y, 0f);
                        break;
                    case FlipStrategy.Scaling:
                        if (FlipSubject.localScale.y > 0)
                            FlipSubject.localScale = new Vector2(FlipSubject.localScale.x, FlipSubject.localScale.y * -1);
                        break;
                    case FlipStrategy.Sprite:
                        _spriteRenderer.flipY = true;
                        break;
                }
                UpdateVerticalDirection(1);
                return;
            }
        }

        /// <summary>
        /// Evaluates if GameObject should be Flipped
        /// </summary>
        /// <param name="subjectDirection"></param>
        /// <returns></returns>
        protected virtual bool ShouldFlipHorizontally(int subjectDirection)
        {
            // Debug.Log($"SubjectDirection: {subjectDirection} _currentHorizontalDirectionSign: {_currentHorizontalDirectionSign}");
            return subjectDirection > 0 && _currentHorizontalDirectionSign < 0 || subjectDirection < 0 && _currentHorizontalDirectionSign > 0;
        }

        /// <summary>
        /// Evaluates if GameObject should be Flipped
        /// </summary>
        /// <param name="subjectDirection"></param>
        /// <returns></returns>
        protected virtual bool ShouldFlipVertically(int subjectDirection)
        {
            return subjectDirection > 0 && _currentVerticalDirectionSign < 0 || subjectDirection < 0 && _currentVerticalDirectionSign > 0;
        }

        /// <summary>
        /// Locks the behaviour. Won't be able to flip
        /// until unlocked
        /// </summary>
        /// <param name="shouldLock"></param>
        public void SetLock(bool shouldLock)
        {
            _locked = shouldLock;
        }

        #endregion

        #region Enums        

        public enum FlipStrategy
        {
            Scaling,
            Rotating,
            Sprite,
        }

        private enum HorizontalDirection
        {
            Left,
            Right,
        }

        private enum VerticalDirection
        {
            Up,
            Down,
        }

        #endregion
    }
}
