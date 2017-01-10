using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomMenu.Tools {

    /// <summary>
    /// This enum value determines the kind of calculation to tween the animations of the buttons in the method TweenPosition()
    /// </summary>
    public enum ETweenMode { LINEAR, EASE_IN, CURVE }

    /// <summary>
    /// This enum can have as many values needed in the gameplay
    /// This is where designers should define their own Context
    /// </summary>
    public enum EContext {
        None = 0,
        Start,
        Movements,
        Combat,
        Pawn,
        Board,
        Object,
        Other,
    }

    public static class MenuHelper {

        #region Tweening Methods

        /// <summary>
        /// Interpolates a Vector3 position with 2 points and a delta time ratio (between 0 and 1)
        /// Choses between 1 of 3 tweening modes (see TweeningMode enum field) and call the appropriate method
        /// </summary>
        /// <param name="_startPoint">The starting Vector3 position of the animation</param>
        /// <param name="_targetPoint">The ending Vector3 position</param>
        /// <param name="_delta">a delta time used to interpolate the current position</param>
        /// <returns>the current Vector3 position resulting of the calculation</returns>
        public static Vector3 TweenPosition(ETweenMode _TweenMode, Vector3 _startPoint, Vector3 _targetPoint, float _delta, AnimationCurve _curve = null) {

            Vector3 movingPosition = Vector3.zero;

            switch (_TweenMode) {
                case ETweenMode.LINEAR:
                    movingPosition = Linear(_startPoint, _targetPoint, _delta);
                    break;

                case ETweenMode.EASE_IN:
                    movingPosition = EaseIn(_startPoint, _targetPoint, _delta);
                    break;

                case ETweenMode.CURVE:
                    movingPosition = Curve(_startPoint, _targetPoint, _delta, _curve);
                    break;

                default:
                    break;
            }

            return movingPosition;
        }

        /// <summary>
        /// Simple linear interpolation of a Vector3 position
        /// </summary>
        /// <param name="_startPoint">The starting Vector3 position of the animation</param>
        /// <param name="_targetPoint">The ending Vector3 position</param>
        /// <param name="_delta">a delta time used to interpolate the current position</param>
        /// <returns>the current Vector3 position resulting of the calculation</returns>
        private static Vector3 Linear(Vector3 _startPoint, Vector3 _targetPoint, float _delta) {

            return Vector3.Lerp(_startPoint, _targetPoint, _delta);
        }


        /// <summary>
        /// Simple squared interpolation of a Vector3 position
        /// </summary>
        /// <param name="_startPoint">The starting Vector3 position of the animation</param>
        /// <param name="_targetPoint">The ending Vector3 position</param>
        /// <param name="_delta">a delta time used to interpolate the current position</param>
        /// <returns>the current Vector3 position resulting of the calculation</returns>
        private static Vector3 EaseIn(Vector3 _startPoint, Vector3 _targetPoint, float _delta) {

            return Vector3.Lerp(_startPoint, _targetPoint, _delta * _delta);
        }


        /// <summary>
        /// Interpolation of a Vector3 position based on a curve as defined by the TweeningCurve Animation Curve
        /// </summary>
        /// <param name="_startPoint">The starting Vector3 position of the animation</param>
        /// <param name="_targetPoint">The ending Vector3 position</param>
        /// <param name="_delta">a delta time used to interpolate the current position</param>
        /// <returns>the current Vector3 position resulting of the calculation</returns>
        private static Vector3 Curve(Vector3 _startPoint, Vector3 _targetPoint, float _delta, AnimationCurve _curve) {

            return (_targetPoint - _startPoint) * _curve.Evaluate(_delta) + _startPoint;
        }

        #endregion Tweening Methods

    }
}
