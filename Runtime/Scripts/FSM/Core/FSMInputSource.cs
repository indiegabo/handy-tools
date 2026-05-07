using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Base component used to feed runtime input values into an FSM brain.
    /// </summary>
    public abstract class FSMInputSource : MonoBehaviour
    {
        #region Fields

        private FSMBrain _brain;

        #endregion

        #region Getters

        /// <summary>
        /// Gets the brain currently receiving values from this source.
        /// </summary>
        protected FSMBrain Brain => _brain;

        /// <summary>
        /// Gets whether this source is currently bound to a brain.
        /// </summary>
        protected bool IsBound => _brain != null;

        #endregion

        #region Unity Messages

        /// <summary>
        /// Auto-links this source to a brain on the same GameObject when the
        /// component is first added.
        /// </summary>
        private void Reset()
        {
            TryAssignLocalBrain();
        }

        /// <summary>
        /// Keeps the local brain reference synchronized in the editor when
        /// components are added or reconfigured on the same GameObject.
        /// </summary>
        private void OnValidate()
        {
            TryAssignLocalBrain();
        }

        #endregion

        #region Binding

        /// <summary>
        /// Binds this source to one target brain.
        /// </summary>
        /// <param name="brain">Brain that will receive future values.</param>
        internal void BindToBrain(FSMBrain brain)
        {
            if (_brain == brain)
            {
                return;
            }

            if (_brain != null)
            {
                UnbindFromBrain(_brain);
            }

            _brain = brain;
            OnBound(brain);
        }

        /// <summary>
        /// Unbinds this source from one target brain.
        /// </summary>
        /// <param name="brain">Brain that should stop receiving values.</param>
        internal void UnbindFromBrain(FSMBrain brain)
        {
            if (_brain != brain)
            {
                return;
            }

            OnUnbinding(brain);
            _brain = null;
        }

        /// <summary>
        /// Called after this source was bound to a target brain.
        /// </summary>
        /// <param name="brain">Brain that will receive future values.</param>
        protected virtual void OnBound(FSMBrain brain)
        {
        }

        /// <summary>
        /// Called right before this source is detached from its target brain.
        /// </summary>
        /// <param name="brain">Brain that is about to stop receiving values.</param>
        protected virtual void OnUnbinding(FSMBrain brain)
        {
        }

        /// <summary>
        /// Tries to assign this source into a brain that lives on the same
        /// GameObject.
        /// </summary>
        private void TryAssignLocalBrain()
        {
            FSMBrain brain = GetComponent<FSMBrain>();
            brain?.TryAssignInputSource(this);
        }

        #endregion

        #region Reporting

        /// <summary>
        /// Pushes one button value into the bound brain.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="displayName">Display name used by diagnostics.</param>
        /// <param name="value">Current button value.</param>
        protected void ReportButton(Guid actionId, string displayName, bool value)
        {
            _brain?.Input.SetButtonValue(actionId, displayName, value);
        }

        /// <summary>
        /// Pushes one float value into the bound brain.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="displayName">Display name used by diagnostics.</param>
        /// <param name="value">Current float value.</param>
        protected void ReportFloat(Guid actionId, string displayName, float value)
        {
            _brain?.Input.SetFloatValue(actionId, displayName, value);
        }

        /// <summary>
        /// Pushes one vector value into the bound brain.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="displayName">Display name used by diagnostics.</param>
        /// <param name="value">Current vector value.</param>
        protected void ReportVector2(
            Guid actionId,
            string displayName,
            Vector2 value)
        {
            _brain?.Input.SetVector2Value(actionId, displayName, value);
        }

        /// <summary>
        /// Pushes the semantic movement input used by the optional CCPro
        /// movement-reference runtime into the bound brain.
        /// </summary>
        /// <param name="value">Current semantic movement input.</param>
        protected void ReportMovementInput(Vector2 value)
        {
            _brain?.Input.SetReportedMovementInput(value);
        }

        /// <summary>
        /// Clears the semantic movement input used by the optional CCPro
        /// movement-reference runtime.
        /// </summary>
        protected void ClearMovementInput()
        {
            _brain?.Input.ClearReportedMovementInput();
        }

        /// <summary>
        /// Clears every cached value currently owned by the bound brain.
        /// </summary>
        protected void ClearInputs()
        {
            _brain?.Input.ClearValues();
            _brain?.Input.ClearReportedMovementInput();
        }

        #endregion
    }
}