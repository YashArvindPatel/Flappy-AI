using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(NeuralNetwork))]
public class Bird : MonoBehaviour 
{
	public float upForce = 150;				
	private bool isDead = false;		

	private Animator anim;				
	private Rigidbody2D rb2d;				
    public NetworkManager manager;
    public NeuralNetwork network;

    [Header("Network")]
    public int genome;
    public float aSensor;
    public float bSensor;
    public float cSensor;
    public float dSensor;
    public float eSensor;

    [Header("Fitness")]
    public float overallFitness = 0;
    public float timeMultiplier = 10;
    public float scoreMultiplier = 50;
    public float sensorMultiplier = 0;

    [Header("Information")]
    public float timeTravelled = 0;
    public int scoreCounter = 0;

    [Header("Extra")]
    public Vector3 startPosition;
    public Vector3 startRotation;

    public float output;

    void Start()
	{
		anim = GetComponent<Animator> ();
		rb2d = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        manager = FindObjectOfType<NetworkManager>();
	}

	void Update()
	{
		if (isDead == false) 
		{            
            aSensor = GetComponent<Rigidbody2D>().velocity.y / 10;
            bSensor = transform.position.y / 10;
            cSensor = manager.upper.x / 10;
            dSensor = manager.upper.y / 10;
            eSensor = manager.lower.y / 10;

            output = network.RunNetwork(aSensor, bSensor, cSensor, dSensor, eSensor);

            if (output >= 0.5)
			{
				anim.SetTrigger("Flap");
				rb2d.velocity = Vector2.zero;
				rb2d.AddForce(new Vector2(0, upForce));
			}

            timeTravelled += Time.deltaTime;

            CalculateFitness();
		}
	}

    void CalculateFitness()
    {
        overallFitness = timeMultiplier * timeTravelled;
    }

    void Death()
    {
        manager.FeedDataOnDeath(genome, overallFitness + scoreCounter * scoreMultiplier);
        Reset();
    }

    void Reset()
    {
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
        overallFitness = 0;
        timeTravelled = 0;
        scoreCounter = 0;
        cSensor = dSensor = eSensor = 0;       
    }

    void OnCollisionEnter2D(Collision2D other)
	{
        if (other.gameObject.tag != "Birdy")
        {
            Death();
        }   
	}
}
