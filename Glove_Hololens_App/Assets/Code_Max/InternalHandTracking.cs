using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class InternalHandTracking : MonoBehaviour
{

    private GameObject indicator = null;
    private TextMesh textMesh = null;

    private void CreateIndicator()
    {
        if (indicator == null)
        {
            indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
        }
    }

    private void UpdateIndicator(Vector3 position)
    {
        if (indicator != null)
        {
            indicator.transform.position = position;
        }
    }

    private void CreateText()
    {
        GameObject text = new GameObject();
        textMesh = text.AddComponent<TextMesh>() as TextMesh;
        text.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    private void UpdateText(Vector3 position, Vector3 velocity)
    {
        if (textMesh != null)
        {
            position = new Vector3(position.x, position.y + 0.1f, position.z);

            textMesh.gameObject.transform.position = position;
            var gazeDirection = Camera.main.transform.forward;
            textMesh.gameObject.transform.rotation = Quaternion.LookRotation(gazeDirection);
            textMesh.text = string.Format("Position:{0:0.00},{1:0.00},{2:0.00}\n Velocity: {3:0.00},{4:0.00},{5:0.00}", position.x, position.y, position.z, velocity.x, velocity.y, velocity.z);
        }
    }

    public void ShowObjects(bool show)
    {
        if (indicator != null && textMesh != null)
        {
            indicator.SetActive(show);
            textMesh.gameObject.SetActive(show);
        }
    }

    // Use this for initialization
    void Start()
    {
        Debug.Log("HandTracking started");

        CreateIndicator();
        CreateText();

        InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;
        InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
        InteractionManager.InteractionSourceUpdated += InteractionManager_InteractionSourceUpdated;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs obj)
    {
        if (obj.state.source.kind == InteractionSourceKind.Hand)
        {
            Debug.Log("Hand detected");

            ShowObjects(true);
        }
    }

    private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs obj)
    {
        if (obj.state.source.kind == InteractionSourceKind.Hand)
        {
            ShowObjects(false);
        }
    }

    private void InteractionManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs obj)
    {
        if (obj.state.source.kind == InteractionSourceKind.Hand)
        {
            Vector3 handPosition;
            Vector3 handVelocity;

            obj.state.sourcePose.TryGetPosition(out handPosition);
            obj.state.sourcePose.TryGetVelocity(out handVelocity);

            UpdateText(handPosition, handVelocity);
            UpdateIndicator(handPosition);
        }
    }
}
