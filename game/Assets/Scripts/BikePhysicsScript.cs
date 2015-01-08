using UnityEngine;
using System.Collections;

public class BikePhysicsScript : MonoBehaviour
{
	Socket Network;

    public GameObject centerOfMass;
    public GameObject frontWheel;
    public GameObject rearWheel;
	public Transform modelFrontFork;

    public TextGUIScript TextGUI;
	public Trail trailScript;

    public float gravity;
	public bool canRoll;

    private float forkRotation;
    private float speed;

	private float rotRadius;

    private float rollAngularSpeed;
    private float rollAngularAcc;
    private float rotAngularSpeed;

	private const float maxSteerRotation = 80.0f;
	private const float minSteerRotation = -80.0f;

    //TODO delete this!
    private float rotationUnit = 0.5f;
    private float speedUnit = 0.1f;
	private float brakeUnit = 0.5f;

    // Use this for initialization
    void Start()
    {
		rotRadius = 0;
        rollAngularSpeed = 0.0f;
        rollAngularAcc = 0.0f;

        forkRotation = 0;
        speed = 0;

		Network = GameObject.Find ("Network").GetComponent<Socket> ();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (MenuSelection.state != GameState.Playing)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

		
		forkRotation = ApplyMaxMinRotation(Network.getParsedAngle());
  
		float networkSpeed = Network.getParsedSpeed ();


		float f = Input.GetAxis("Fire1");

		if(f ==1) {
			ResetBike();
			trailScript.Reset();
		}

		if (speed < networkSpeed)
			speed = speed + speedUnit;
		else //if (speed > networkSpeed)
			speed = speed - speedUnit;

		if (Network.getParsedBrake() == 1) {
			speed = speed - brakeUnit;
			if (speed < 0)
				speed = 0;
		}


        // Moving and rotation part
        //float angleSpeed = Mathf.Abs(forkRotation);

        Vector3 vector = Quaternion.Euler(0, this.transform.rotation.y, 0) * Quaternion.Euler(this.transform.rotation.eulerAngles) * Vector3.forward;

        Quaternion angle = Quaternion.Euler(0, 1, 0) * this.transform.rotation;

        this.transform.position = Vector3.MoveTowards(this.transform.position,
                                                      this.transform.position + vector,
                                                      speed * Time.deltaTime);

        this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation,
                                                           angle,
                                                           rotAngularSpeed);

        //Rolling and gravity part
        ApplyGravity(); //makes sure the wheels are against the floor.. maybe it can be removed
		ApplyRotation(); //makes the bike to steer
        ApplyInstability(); //if the center of mass isn't over the wheels, it will fall

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
		rollAngularAcc = 0;

        if (this.transform.rotation.eulerAngles.z != 0)
        {
            rollAngularAcc = gravity * Mathf.Sin(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.z) / centerOfMass.transform.position.y;
        }

		if(rotRadius != 0) {
			rollAngularAcc +=speed*speed / (rotRadius * centerOfMass.transform.position.y * centerOfMass.transform.position.y);

		}

        if (rollAngularAcc != 0)
        {
            rollAngularSpeed += rollAngularAcc * Time.deltaTime;
        }

		//TODO: improve this
        if (rollAngularSpeed != 0 && canRoll)
        {
            Vector3 angleEuler = this.transform.rotation.eulerAngles;
            angleEuler.z += 1;
            Quaternion angle = Quaternion.Euler(angleEuler);

            this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation,
                                                               angle,
                                                               rollAngularSpeed);
        }
    }

    void ApplyRotation()
    {

        float difWheel = frontWheel.transform.localPosition.z;
        if (forkRotation != 0)
        {
			rotRadius = difWheel / Mathf.Tan(forkRotation * Mathf.Deg2Rad);
        } else {
			rotRadius = 0;
		}

		if (rotRadius != 0)
        {
			rotAngularSpeed = speed / rotRadius;
        } else {
			rotAngularSpeed = 0;
		}
    }


    void UpdateRotation(float rot)
    {
        if (rot != 0)
        {
			if (rot > 0 && forkRotation < maxSteerRotation)
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
				if (forkRotation > minSteerRotation)
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
    }

	float ApplyMaxMinRotation(float rotation)
	{
		if (rotation > maxSteerRotation)
						return maxSteerRotation;
				else if (rotation < minSteerRotation)
						return minSteerRotation;
				else
						return rotation;
	}

    void UpdateSpeed(float force)
    {
        if (force != 0)
        {
            speed += force * speedUnit;
        }
    }

	void ResetBike() {
		ResetBikeRoll();
		ResetBikePos();
		ResetBikeSteerAngle();
		ResetBikeSpeed();
	}

	void ResetBikeRoll() {
		rollAngularSpeed = 0.0f;
		Vector3 angleEuler = this.transform.rotation.eulerAngles;
		angleEuler.z = 0;
		Quaternion angle = Quaternion.Euler(angleEuler);
		
		this.transform.rotation = angle;
	}

	void ResetBikePos() {
		this.transform.position = new Vector3 (0, 0, 0);
		this.transform.rotation = Quaternion.Euler (new Vector3 (0, 0, 0));
	}

	void ResetBikeSpeed() {
		speed = 0.0f;
	}

	void ResetBikeSteerAngle() {
		forkRotation = 0.0f;
	}

    void LateUpdate()
    {
        TextGUI.UpdateBikeValuesText(forkRotation, speed);
    }
}
