using UnityEngine;
using System.Collections;

public class BikePhysicsScript : MonoBehaviour
{

    public GameObject centerOfMass;
    public GameObject frontWheel;
    public GameObject rearWheel;

    public TextGUIScript TextGUI;

    public float gravity;
    public Transform modelFrontFork;

    private float forkRotation;
    private float speed;
    //private float bikeRotation;
    //private float currentPosition;
    //private float bikeRollingAngle;

    private float RollAngularSpeed;
    private float RollAngularAcc;

    private float RotAngularSpeed;

    //TODO delete this!
    private float rotationUnit = 0.5f;
    private float speedUnit = 0.1f;

    // Use this for initialization
    void Start()
    {

        RollAngularSpeed = 0.0f;
        RollAngularAcc = 0.0f;

        forkRotation = 0;
        speed = 0;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (MenuSelection.state != GameState.Playing)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        UpdateRotation(h);
        UpdateSpeed(v);


        // Moving and rotation part
        float angleSpeed = Mathf.Abs(forkRotation);

        Vector3 vector = Quaternion.Euler(0, this.transform.rotation.y, 0) * Quaternion.Euler(this.transform.rotation.eulerAngles) * Vector3.forward;

        Quaternion angle = Quaternion.Euler(0, 1, 0) * this.transform.rotation;

        this.transform.position = Vector3.MoveTowards(this.transform.position,
                                                      this.transform.position + vector,
                                                      speed * Time.deltaTime);

        this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation,
                                                           angle,
                                                           RotAngularSpeed);

        //Rolling and gravity part
        ApplyGravity();
        ApplyInstability();
        ApplyRotation();

        modelFrontFork.localRotation = Quaternion.AngleAxis(forkRotation, new Vector3(0, 3, -1));
    }

    void ApplyGravity()
    {

        if (frontWheel.transform.position.y != 0)
        {
            Vector3 vector;
            if (frontWheel.transform.position.y < 0)
            {
                vector = Vector3.up;
            }
            else
            {
                vector = Vector3.down;
            }
            vector *= Mathf.Abs(frontWheel.transform.position.y);
            this.transform.position = Vector3.MoveTowards(this.transform.position,
                                                          this.transform.position + vector,
                                                          2 * Time.deltaTime);
        }
    }

    void ApplyInstability()
    {

        //Debug.Log (RollAngularAcc);

        if (this.transform.rotation.eulerAngles.z != 0)
        {
            RollAngularAcc = gravity * Mathf.Sin(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.z) / centerOfMass.transform.position.y;
        }

        if (RollAngularAcc != 0)
        {
            RollAngularSpeed += RollAngularAcc * Time.deltaTime;
        }

        if (RollAngularSpeed != 0)
        {
            Vector3 angleEuler = this.transform.rotation.eulerAngles;
            angleEuler.z += 1;
            Quaternion angle = Quaternion.Euler(angleEuler);

            this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation,
                                                               angle,
                                                               RollAngularSpeed);
        }
    }

    void ApplyRotation()
    {

        float difWheel = frontWheel.transform.localPosition.z;
        float radius = 0;
        if (forkRotation != 0)
        {
            radius = difWheel / Mathf.Tan(forkRotation * Mathf.Deg2Rad);
        }

        if (radius != 0)
        {
            RotAngularSpeed = speed / radius;
        }

        Debug.Log(RotAngularSpeed);
    }


    void UpdateRotation(float rot)
    {
        if (rot != 0)
        {
            if (rot > 0)
            {
                if (forkRotation > 0)
                {
                    forkRotation += 1 * rotationUnit;
                }
                else
                {
                    forkRotation += 3 * rotationUnit;
                }
            }
            else
            {
                if (forkRotation < 0)
                {
                    forkRotation -= 1 * rotationUnit;
                }
                else
                {
                    forkRotation -= 3 * rotationUnit;
                }
            }
        }
    }

    void UpdateSpeed(float force)
    {
        if (force != 0)
        {
            speed += force * speedUnit;
        }
    }

    void LateUpdate()
    {
        TextGUI.UpdateBikeValuesText(forkRotation, speed);
    }
}
