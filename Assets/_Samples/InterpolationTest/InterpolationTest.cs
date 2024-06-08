using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class InterpolationTest : MonoBehaviour
{
    public Transform mainPlayer;
    public float mainPlayerSpeed = 1;

    public Transform clonePlayer;
    public float sendRate = 10;
    public int latency_ms = 50;
    [SerializeField]
    Vector3 targetPoint;

    private InterpolationController interpolationController;
    void Start()
    {
        Application.targetFrameRate = 60;
        interpolationController = new InterpolationController();
        interpolationController.AssignTransform(clonePlayer);
        StartCoroutine(SendUpdateToClonePlaye());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray,out RaycastHit hit))
            {
                targetPoint = hit.point;
            }
        }
    }

    IEnumerator SendUpdateToClonePlaye()
    {
        while(true)
        {
            UpdateClonePlayer(mainPlayer.position, Time.time);
            float timer = 1f / sendRate;
            while(timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
        }
    }

   

    private void LateUpdate()
    {
        mainPlayer.position = Vector3.MoveTowards(mainPlayer.position, targetPoint, mainPlayerSpeed * Time.deltaTime);
        interpolationController.Interpolation();
    }



   
    private async void UpdateClonePlayer(Vector3 position, float time)
    {
        await Task.Delay(latency_ms);
        interpolationController.Update(position, time);
       
       
    }


}

public class InterpolationController
{
    Transform transform;
    private float lastRecordedTime = -1;

    public void AssignTransform(Transform transform)
    {
        this.transform = transform;
    }

    InterpolationVariable parameter;
    
    public void Update(Vector3 position, float time)
    {
        
        parameter = LNSInterpolation.CalcuateInterpolationVariables(transform.position, position, time - lastRecordedTime);
        lastRecordedTime = time;
    }

    public void Interpolation()
    {
        //LNSInterpolation.InterpolateVector3(parameter.from,);
        if (parameter.hasValue)
        {
            transform.position = LNSInterpolation.InterpolateVector3(parameter);
            parameter.UpdateTimer(Time.deltaTime);

            //transform.position = Vector3.MoveTowards(transform.position, parameter.to, Time.deltaTime * parameter.speed);
        }
        
        
    }
}


