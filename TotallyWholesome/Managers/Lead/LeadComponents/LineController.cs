﻿using System;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using TotallyWholesome.Notification;
using TotallyWholesome.Objects;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace TotallyWholesome.Managers.Lead.LeadComponents
{
    [DefaultExecutionOrder(9999)]
    public class LineController : MonoBehaviour
    {
        private static readonly int ColourOne = Shader.PropertyToID("_ColourOne");
        private static readonly int ColourTwo = Shader.PropertyToID("_ColourTwo");
        
        public Transform target;
        public Transform targetOverride;
        public Vector3 targetOverrideVector;
        public LineRenderer line;
        public bool IsAtMaxLeashLimit;
        private TWPlayerObject _ourPlayer;
        private TWPlayerObject _targetPlayer;
        private float _maxDistance;
        private float _breakDistance;
        private float _sagAmplitude;
        private Vector3 _sagDirection;
        private int _lineSteps;
        private bool _isReset = false;
        private bool _leashBroken = false;
        private bool _noVisibleLeash = false;
        private bool _tempUnlockLeash = false;
        private Color _masterColour;
        private Color _petColour;
        private static readonly int ShowLead = Shader.PropertyToID("_ShowLead");

        public void SetupRenderer(Transform target, TWPlayerObject ourPlayer, TWPlayerObject targetPlayer, LineRenderer line, float breakDistance, int intermediateSteps, float maxDistance, float sagAmplitude, bool noVisibleLeash, Color masterColour, Color petColour)
        {
            line.enabled = true;

            this.target = target;
            this.line = line;
            _ourPlayer = ourPlayer;
            _breakDistance = breakDistance;
            _targetPlayer = targetPlayer;
            _maxDistance = maxDistance;
            _sagAmplitude = sagAmplitude;
            _lineSteps = intermediateSteps + 2;
            _sagDirection = Physics.gravity.normalized;
            _noVisibleLeash = noVisibleLeash;

            if (!_noVisibleLeash)
            {
                line.textureMode = LineTextureMode.RepeatPerSegment;

                //Allow line to cast shadows
                line.shadowCastingMode = ShadowCastingMode.On;

                line.positionCount = _lineSteps;
                line.material.SetFloat(ShowLead, 1);

                line.startWidth = .015f;
                line.endWidth = .015f;
            }
            else
            {
                line.material.SetFloat(ShowLead, 0);
                line.positionCount = 0;
                line.enabled = false;
            }
            
            UpdateLineColours(masterColour, petColour);
        }
        
        public void ResetRenderer()
        {
            target = null;
            
            if (line != null)
            {
                line.positionCount = 0;
                line.enabled = false;
                line.material.SetFloat(ShowLead, 0);
            }

            _isReset = true;
        }
        
        public void UpdateLeadLength(float length)
        {
            _maxDistance = length;
        }

        public void UpdateLineMaterial(Material material, LineTextureMode textureMode = LineTextureMode.RepeatPerSegment)
        {
            line.material = material;
            line.textureMode = textureMode;
            
            ApplyColours();
        }

        public void UpdateLineColours(Color? masterColour = null, Color? petColour = null)
        {
            if(masterColour.HasValue)
                _masterColour = masterColour.Value;
            if(petColour.HasValue)
                _petColour = petColour.Value;

            ApplyColours();
        }

        private void ApplyColours()
        {
            line.material.SetColor(ColourTwo, _masterColour);
            line.material.SetColor(ColourOne, _petColour);
        }
        
        private void CheckLeashBreakDistance(float currentDistance)
        {
            if (_tempUnlockLeash) return;
            
            if (currentDistance >= _breakDistance && !_leashBroken)
            {
                if(_ourPlayer != null)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "You are too far from your master, the leash has been temporarily broken!", 5f, TWAssets.Link);
                
                BreakLeash();
                _leashBroken = true;
            }

            if (currentDistance <= _breakDistance && _leashBroken)
            {
                _leashBroken = false;
                AttachLeash();
            }
        }
        
        public void SetTempUnlockLeash(bool unlock)
        {
            if (unlock)
            {
                _tempUnlockLeash = true;
                _leashBroken = false;
                BreakLeash();
            }
            else
            {
                //Don't need to reattach
                if (!_tempUnlockLeash)
                    return;
                
                _tempUnlockLeash = false;
                AttachLeash();
            }
        }

        private void AttachLeash()
        {
            if (_leashBroken || _tempUnlockLeash)
                return;
            
            //Reattach leash
            if (!_noVisibleLeash)
            {
                line.positionCount = _lineSteps;
                line.material.SetFloat(ShowLead, 1);
            }
        }

        private void BreakLeash()
        {
            if (!_noVisibleLeash)
            {
                line.positionCount = 0;
                line.material.SetFloat(ShowLead, 0);
            }
        }

        public void LateUpdate()
        {
            if (line == null)
                return;
            
            if (target != null && target.gameObject.activeSelf)
            {
                // Get direction Vector.
                var localUserPosition = transform.position;
                var targetPosition = target.position;

                if (targetOverrideVector != Vector3.zero)
                    targetPosition = targetOverrideVector;
                if (targetOverride != null)
                    targetPosition = targetOverride.position;

                Vector3 difference = targetPosition - localUserPosition;
                float currentDistance = Vector3.Distance(localUserPosition, targetPosition);

                if (!_noVisibleLeash)
                {
                    for (int i = 0; i < line.positionCount; i++)
                    {
                        float pointForCalcs = (float)i / (line.positionCount - 1);
                        float effectAtPointMultiplier = Mathf.Sin(pointForCalcs * Mathf.PI);

                        Vector3 pointPosition = difference * pointForCalcs;
                        // Calculate the sag vector for the current point i
                        //Vector3 sagAtPoint = sagDirection * sagAmplitude;
                        //TODO: Change this to subtracting from SagAmplitude over the maxdistance
                        float amplitudeOverDistance =
                            _sagAmplitude - ((currentDistance / _maxDistance) * _sagAmplitude);

                        Vector3 sagAtPoint = _sagDirection * Math.Max(0, amplitudeOverDistance);

                        Vector3 currentPointsPosition =
                            localUserPosition +
                            pointPosition +
                            (Vector3.ClampMagnitude(sagAtPoint, _sagAmplitude)) * effectAtPointMultiplier;

                        line.SetPosition(i, currentPointsPosition);
                    }
                }

                IsAtMaxLeashLimit = !_leashBroken && !_tempUnlockLeash && MovementSystem.Instance.canMove && currentDistance > _maxDistance;

                CheckLeashBreakDistance(currentDistance);

                if (_ourPlayer == null) return;
                
                //Follower movement
                if (currentDistance > _maxDistance && !_leashBroken && !_tempUnlockLeash && MovementSystem.Instance.canMove)
                {
                    float maxSpeed = MovementSystem.Instance.baseMovementSpeed * MovementSystem.Instance.sprintMultiplier;
                    // if (MetaPort.Instance.isUsingVr && Input.GetKey(KeyCode.LeftShift))
                    //     maxSpeed = MovementSystem.Instance.baseMovementSpeed;

                    float speed = Mathf.Clamp(2f * (currentDistance-_maxDistance), 1.5f, maxSpeed);

                    var targetMixedPosition = targetPosition;
                    var position = _ourPlayer.PlayerPosition;

                    targetMixedPosition.y = position.y;

                    var diff = (targetMixedPosition - position).normalized;

                    MovementSystem.Instance.controller.Move(diff * (speed*Time.deltaTime));
                }
            }
            else
            {
                if (!_isReset)
                    ResetRenderer();
            }
        }
    }
}