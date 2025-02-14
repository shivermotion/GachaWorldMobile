using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UnityEngine.Events;    // For UnityEvent callbacks

[RequireComponent(typeof(CanvasGroup))]
public class BottomSheetController : MonoBehaviour
{
    [Header("Canvas Setup")]
    [Tooltip("Canvas that contains this bottom sheet.")]
    public Canvas parentCanvas;

    [Header("Detents")]
    [Tooltip("Detent ratios (0.0 to 1.0) for the sheet to snap to.")]
    public float[] detentRatios = new float[] { 0.4f, 0.7f, 0.94f };
    private int currDetentIdx = 0;

    [Header("Animation Settings")]
    [Tooltip("Speed in pixels/second. How fast the sheet travels.")]
    public float animPixelsPerSecond = 1920f * 3.5f;

    [Tooltip("Animation curve for elastic/bounce effect.")]
    public AnimationCurve bounceCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 1.5f),
        new Keyframe(0.8f, 1.05f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    [Tooltip("Enable or disable bounce animation.")]
    public bool useElasticBounce = true;

    [Header("Swipe Settings")]
    [Tooltip("Minimum swipe speed (pixels/second) to switch detents.")]
    public float swipeToToggleSpeed = 500f;

    [Header("Tap Settings")]
    [Tooltip("Max duration (seconds) of a touch for it to count as a tap.")]
    public float maxTapDuration = 0.2f;

    [Header("Optional Close Button")]
    [Tooltip("Assign a Button that explicitly closes and destroys the sheet.")]
    public Button closeButton;

    [Header("Events")]
    public UnityEvent onSheetOpened;   // Fires when sheet reaches its highest detent
    public UnityEvent onSheetClosed;   // Fires when sheet returns to lowest detent (e.g., 40%)

    // Internal State
    private bool isTouching = false;
    private bool didDrag = false;
    private float tapDuration = 0f;

    private bool isOpen = false;

    private List<float> swipeCache = new List<float>();
    private Vector2 prevTouchPoint;
    private Vector3 touchMatchmovePosition;
    private float hdScalar = 1f;

    // Since we're removing auto-destroy on swipe, we keep track of "closed" in terms of min ratio.
    private bool isFullyClosed => Mathf.Approximately(detentRatios[currDetentIdx], detentRatios[0]);
    private bool isFullyOpened => Mathf.Approximately(detentRatios[currDetentIdx], detentRatios[detentRatios.Length - 1]);

    private void Awake()
    {
        // Increase frame rate for smoother animations (optional)
        Application.targetFrameRate = 90;

        // Optional: Hook up a close button if assigned
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnUserClosedSheet);
        }
    }



    private void Start()
    {

        // Ensure the bottom sheet starts at its lowest position
        transform.position = GetDetentWorldPosition(detentRatios[0]);

        // Hook up close button
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseBottomSheet);

        // Sort detentRatios so the lowest is first, highest is last
        Array.Sort(detentRatios);

        // If we somehow lost the array, revert to default
        if (detentRatios.Length == 0)
        {
            detentRatios = new float[] { 0.4f, 0.94f };
        }

        // Calculate a scale factor to keep the animations consistent
        CanvasScaler canvasScaler = parentCanvas.GetComponent<CanvasScaler>();
        if (canvasScaler != null)
        {
            Vector2 refRes = canvasScaler.referenceResolution;
            hdScalar = refRes.y / Screen.height;
        }
        else
        {
            hdScalar = 1f;
        }

        // Start hidden at the lowest ratio
        currDetentIdx = 0;
        transform.position = GetDetentWorldPosition(detentRatios[0]);
        // If you want it to animate in from below the screen:
        // transform.position = GetHiddenPosition(); // Then AnimateToDetentRatioWithIndex(0);

        // Snap (or animate) to the initial detent
        AnimateToDetentRatioWithIndex(currDetentIdx);
    }

    /// <summary>
    /// Opens the bottom sheet by animating to the first detent
    /// </summary>
    public void OpenBottomSheet()
    {
        if (!isOpen)
        {
            isOpen = true;
            AnimateToDetentRatioWithIndex(1); // Moves to 70% (change if needed)
        }
    }

    /// <summary>
    /// Closes the bottom sheet by animating to the lowest detent
    /// </summary>
    public void CloseBottomSheet()
    {
        if (isOpen)
        {
            isOpen = false;
            AnimateToDetentRatioWithIndex(0); // Moves back to 40% detent
        }
    }



    private void Update()
    {
        if (isTouching)
        {
            tapDuration += Time.deltaTime;
            // Smoothly follow the finger to reduce stutter
            transform.position = Vector3.Lerp(transform.position, touchMatchmovePosition, Time.deltaTime * 20f);
        }
    }

    #region Pointer Event Handlers

    public void OnPointerDown(BaseEventData baseEventData)
    {
        // Begin tracking for tap detection
        isTouching = true;
        didDrag = false;
        tapDuration = 0f;

        // Prepare for matchmove
        touchMatchmovePosition = transform.position;

        // Initialize swipe tracking
        swipeCache.Clear();
        PointerEventData pointerEventData = (PointerEventData)baseEventData;
        prevTouchPoint = pointerEventData.position;
    }

    public void OnPointerUp(BaseEventData baseEventData)
    {
        isTouching = false;

        // If the user didn't drag AND it was a short press -> it's a tap
        if (!didDrag && tapDuration <= maxTapDuration)
        {
            OnTap();
        }
    }

    public void OnDrag(BaseEventData baseEventData)
    {
        didDrag = true;
        PointerEventData pointerEventData = (PointerEventData)baseEventData;
        Vector2 touchPoint = pointerEventData.position;

        // Move the menu along with the touch
        MatchmoveMenuWithTouch(touchPoint);

        // Track swipe speeds
        TrackSwipeEvents(touchPoint);
    }

    public void OnEndDrag(BaseEventData baseEventData)
    {
        // Calculate average vertical speed over recent frames
        float totalDelta = 0f;
        for (int i = 0; i < swipeCache.Count; i++)
        {
            totalDelta += swipeCache[i];
        }

        // If we had zero frames of movement, skip
        float averageDeltaPixels = (swipeCache.Count > 0) ? (totalDelta / swipeCache.Count) : 0f;
        float deltaPixelsPerSecond = averageDeltaPixels / Time.deltaTime;

        if (Mathf.Abs(deltaPixelsPerSecond) >= swipeToToggleSpeed)
        {
            // Swipe Up
            if (deltaPixelsPerSecond > 0)
            {
                AnimateToDetentRatioWithIndex(currDetentIdx + 1);
            }
            // Swipe Down
            else
            {
                AnimateToDetentRatioWithIndex(currDetentIdx - 1);
            }
        }
        else
        {
            AnimateToNearestDetent();
        }
    }

    #endregion

    #region Private Methods

    private void OnTap()
    {
        // Move to the next detent (wrap around)
        currDetentIdx = (currDetentIdx + 1) % detentRatios.Length;
        AnimateToDetentRatio(detentRatios[currDetentIdx]);
    }

    private void MatchmoveMenuWithTouch(Vector2 touchPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)parentCanvas.transform,
            touchPoint,
            parentCanvas.worldCamera,
            out Vector2 rectPoint);

        // Convert localPoint to world point
        Vector3 newWorldPos = parentCanvas.transform.TransformPoint(rectPoint);

        // Only update y-position
        touchMatchmovePosition = transform.position;
        touchMatchmovePosition.y = newWorldPos.y;
    }

    private void TrackSwipeEvents(Vector2 currTouchPoint)
    {
        Vector2 deltaPixels = currTouchPoint - prevTouchPoint;
        swipeCache.Add(deltaPixels.y);
        prevTouchPoint = currTouchPoint;
    }

    private void AnimateToNearestDetent()
    {
        int closestIdx = GetNearestDetentRatioIdx();
        AnimateToDetentRatioWithIndex(closestIdx);
    }

    private int GetNearestDetentRatioIdx()
    {
        // Current ratio
        float currentRatio = transform.position.y / Screen.height;

        int closestIdx = 0;
        float minDist = Mathf.Abs(detentRatios[closestIdx] - currentRatio);

        for (int i = 1; i < detentRatios.Length; i++)
        {
            float dist = Mathf.Abs(detentRatios[i] - currentRatio);
            if (dist < minDist)
            {
                minDist = dist;
                closestIdx = i;
            }
        }

        return closestIdx;
    }

    private void AnimateToDetentRatioWithIndex(int detentIdx)
    {
        // Clamp index so we can’t go below the lowest ratio or above the highest ratio
        detentIdx = Mathf.Clamp(detentIdx, 0, detentRatios.Length - 1);

        currDetentIdx = detentIdx;
        AnimateToDetentRatio(detentRatios[currDetentIdx]);
    }

    private void AnimateToDetentRatio(float targetRatio)
    {
        Vector3 targetPos = GetDetentWorldPosition(targetRatio);
        AnimateToPosition(targetPos);
    }

    private Vector3 GetDetentWorldPosition(float ratio)
    {
        // Start from our current x,z, but set y based on ratio * screen height
        Vector3 pos = transform.position;
        pos.y = ratio * Screen.height;
        return pos;
    }

    // If you want it to initially appear below the screen:
    private Vector3 GetHiddenPosition()
    {
        Vector3 pos = transform.position;
        pos.y = 0f; // or a negative value to place it below visible screen
        return pos;
    }

    private void AnimateToPosition(Vector3 targetPosition)
    {
        StartCoroutine(AnimateToPosition_helper(targetPosition));
    }

    private IEnumerator AnimateToPosition_helper(Vector3 targetPosition)
    {
        float currTime = 0f;
        Vector3 startPosition = transform.position;

        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        float scaledDistance = totalDistance * hdScalar;
        float totalTime = scaledDistance / animPixelsPerSecond;

        // If totalTime is very small, skip the loop to avoid divide-by-zero
        if (totalTime < 0.01f)
        {
            transform.position = targetPosition;
            yield break;
        }

        while (currTime < totalTime)
        {
            currTime += Time.deltaTime;
            float t = currTime / totalTime;

            // If bounce is enabled, evaluate the curve
            if (useElasticBounce && bounceCurve != null)
            {
                float curveValue = bounceCurve.Evaluate(t);
                transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, curveValue);
            }
            else
            {
                // Regular smooth animation
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            }

            yield return null;
        }

        // Snap to final position
        transform.position = targetPosition;

        // Fire open/close events if we've reached the top or bottom detent
        if (isFullyOpened)
        {
            onSheetOpened?.Invoke();
        }
        else if (isFullyClosed)
        {
            onSheetClosed?.Invoke();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Closes and destroys the sheet. 
    /// Assign this to a button’s onClick to manually close the sheet.
    /// </summary>
    public void OnUserClosedSheet()
    {
        // Animate to the lowest ratio
        currDetentIdx = 0;
        AnimateToDetentRatioWithIndex(currDetentIdx);

        // (Optional) Wait a tiny bit, then destroy
        StartCoroutine(DestroyAfterAnimation());
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f); // or check if the position is reached
        Destroy(gameObject);
    }

    #endregion
}
