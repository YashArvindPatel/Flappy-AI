using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    [Header("References")]
    public GameObject playerController3D;
    public GameObject[] players;

    [Header("Controls")]
    public int populationCount = 50;
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.015f;

    [Header("Crossover Controls")]
    public int bestSelection = 8;
    public int numberToCrossover = 10;

    [Header("Public View")]
    public int currentGeneration = 0;
    public int currentGenome = 0;
    public int deadPlayers = 0;

    public List<NeuralNetwork> networks = new List<NeuralNetwork>();
    public int layers, neurons;
    public int startIndex = 0;

    public Text text1, bestFitness, speedText;
    public GameObject ground1, ground2;
    public Vector3 ground1Pos, ground2Pos;
    public Slider slider;

    public Vector3 upper, lower;

    private void Awake()
    {
        players = new GameObject[populationCount];
        ground1Pos = ground1.transform.position;
        ground2Pos = ground2.transform.position;
        CreatePopulation();
    }

    private void Update()
    {
        Time.timeScale = slider.value;
        speedText.text = "Speed: x" + slider.value;

        GameObject[] upperOnes = GameObject.FindGameObjectsWithTag("Upper");
        GameObject[] lowerOnes = GameObject.FindGameObjectsWithTag("Lower");
        float upperMax = 20;
        float lowerMax = 20;

        Vector3 alivePlayerPos = players[0].transform.position;

        foreach (var item in players)
        {
            if (item.activeSelf)
            {
                alivePlayerPos = item.transform.position;
                break;
            }
        }

        foreach (var item in upperOnes)
        {
            if (item.transform.position.x > alivePlayerPos.x && item.transform.position.x < upperMax)
            {
                upperMax = item.transform.position.x;
                upper = item.transform.position;
            }
        }

        foreach (var item in lowerOnes)
        {
            if (item.transform.position.x > alivePlayerPos.x && item.transform.position.x < lowerMax)
            {
                lowerMax = item.transform.position.x;
                lower = item.transform.position;
            }
        }

        Debug.DrawLine(upper, lower);
    }

    public void CreatePopulation()
    {
        while (startIndex < populationCount)
        {
            NeuralNetwork network = new NeuralNetwork();
            GameObject player = Instantiate(playerController3D);
            players[currentGenome] = player;
            network.Initialise(layers, neurons);
            networks.Add(network);
            player.GetComponent<Bird>().network = network;
            player.GetComponent<Bird>().genome = currentGenome;
            currentGenome++;
            startIndex++;
        }

        startIndex = 0;
        currentGenome = 0;
        text1.text = "Generation: " + currentGeneration;      
    }

    public void ResetScene()
    {
        ground1.transform.position = ground1Pos;
        ground2.transform.position = ground2Pos;

        foreach (var item in FindObjectsOfType<Column>())
        {
            Destroy(item.gameObject);
        }

        FindObjectOfType<ColumnPool>().Start();

        Repopulate();

        foreach (var item in players)
        {
            item.SetActive(true);
            item.GetComponent<Bird>().network = networks[currentGenome];
            currentGenome++;
        }

        currentGenome = 0;
    }

    public void FeedDataOnDeath(int genome, float fitness)
    {
        networks[genome].fitness = fitness;
        players[genome].SetActive(false);

        if (System.Convert.ToDouble(bestFitness.text.Substring(14)) < fitness)
        {
            bestFitness.text = "Best Fitness: " + fitness;
        }

        deadPlayers++;

        if(deadPlayers == populationCount)
        {
            deadPlayers = 0;
            ResetScene();
        }
    }

    public void Repopulate()
    {
        SortNetworksAccordingToFitness();

        BetterGenerationCreation();
    }

    public void SortNetworksAccordingToFitness()
    {
        networks = networks.OrderByDescending(x => x.fitness).ToList();
    }

    public void BetterGenerationCreation()
    {
        List<NeuralNetwork> betterNetworks = new List<NeuralNetwork>();

        for (int i = 0; i < bestSelection; i++)
        {
            NeuralNetwork net = networks[i].InitialiseCopy(layers, neurons);
            net.fitness = 0;
            betterNetworks.Add(net);
        }

        CrossOver(betterNetworks);

        Mutate(betterNetworks);

        FillUpRestSpots(betterNetworks);

        networks = betterNetworks;

        currentGenome = 0;
        currentGeneration++;

        text1.text = "Current Generation: " + currentGeneration;
    }

    public void CrossOver(List<NeuralNetwork> betterNet)
    {
        for (int i = 0; i < numberToCrossover -  1; i += 2)
        {
            NeuralNetwork Child1 = new NeuralNetwork();
            NeuralNetwork Child2 = new NeuralNetwork();

            Child1.Initialise(layers, neurons);
            Child2.Initialise(layers, neurons);

            for (int j = 0; j < betterNet[i].weights.Count; j++)
            {
                if (Random.Range(0, 2) == 0)
                {
                    Child1.weights[j] = betterNet[i].weights[j];
                    Child2.weights[j] = betterNet[i + 1].weights[j];
                }
                else
                {
                    Child2.weights[j] = betterNet[i].weights[j];
                    Child1.weights[j] = betterNet[i + 1].weights[j];
                }
            }

            for (int j = 0; j < betterNet[i].biases.Count; j++)
            {
                if (Random.Range(0, 2) == 0)
                {
                    Child1.biases[j] = betterNet[i].biases[j];
                    Child2.biases[j] = betterNet[i + 1].biases[j];
                }
                else
                {
                    Child2.biases[j] = betterNet[i].biases[j];
                    Child1.biases[j] = betterNet[i + 1].biases[j];
                }
            }

            betterNet.Add(Child1);
            betterNet.Add(Child2);
        }
    }

    public void Mutate(List<NeuralNetwork> betterNetworks)
    {
        foreach (var item in betterNetworks)
        {
            for (int i = 0; i < item.weights.Count; i++)
            {
                if (Random.Range(0f,1f) < mutationRate)
                {
                    int randomRow = Random.Range(0, item.weights[i].RowCount);
                    int randomColumn = Random.Range(0, item.weights[i].ColumnCount);

                    item.weights[i][randomRow, randomColumn] = Mathf.Clamp(item.weights[i][randomRow, randomColumn] + Random.Range(-1f, 1f), -1f, 1f);
                }
            }
        }
    }

    public void FillUpRestSpots(List<NeuralNetwork> betterNetworks)
    {
        while (betterNetworks.Count < populationCount)
        {
            NeuralNetwork newNetwork = new NeuralNetwork();
            newNetwork.Initialise(layers, neurons);
            betterNetworks.Add(newNetwork);
        }
    }
}
