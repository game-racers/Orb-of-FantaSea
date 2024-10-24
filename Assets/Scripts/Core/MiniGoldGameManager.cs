using gameracers.Control;
using gameracers.Movement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace gameracers.Core
{
    public class MiniGoldGameManager : MonoBehaviour
    {
        [SerializeField] List<int> HolePar = new List<int>();
        int holeIndex = 0;
        PlayerController player;

        [SerializeField] TextMeshProUGUI hits;
        [SerializeField] GameObject roundOverScreen;
        [SerializeField] GameObject scoreScreen;
        [SerializeField] Transform spawnPoint;
        Vector3 lastHit;


        List<int> score = new List<int>();
        int total;

        float scoreAnnouncementTimer = Mathf.Infinity;
        [SerializeField] float scoreAnnouncementMax = 3f;

        private void OnEnable()
        {
            EventListener.onBallInHole += BallEnteredHole;
            EventListener.onBallHit += BallHit;
        }

        private void OnDisable()
        {
            EventListener.onBallInHole -= BallEnteredHole;
            EventListener.onBallHit -= BallHit;
        }

        private void BallEnteredHole(GameObject entity, string holeID = null)
        {
            if (GameObject.ReferenceEquals(entity, player.gameObject))
            {
                if (holeID == "SCRATCH")
                {
                    Scratch();
                    return;
                }
                score.Add(player.GetSwings());
                total += score[^1];
                player.ResetSwings();
                if (scoreScreen != null)
                {
                    scoreAnnouncementTimer = Time.time;
                    scoreScreen.SetActive(true);
                    scoreScreen.GetComponent<TextMeshProUGUI>().text = CalculateScore();
                }
            }
        }

        private void BallHit(GameObject entity)
        {
            if (GameObject.ReferenceEquals(entity, player.gameObject))
            {
                lastHit = player.transform.position + Vector3.up * .2f;
                if (hits != null)
                    hits.text = player.GetSwings().ToString();
            }
        }

        private void Start()
        {
            player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();

            int parIndex = 0;
            int parTotal = 0;
            Transform parText = roundOverScreen.transform.Find("Par Text");
            foreach (int par in HolePar)
            {
                parText.GetChild(parIndex).GetComponent<TextMeshProUGUI>().text = par.ToString();
                parTotal += par;
                parIndex += 1;
            }

            roundOverScreen.transform.Find("Par Total").GetComponent<TextMeshProUGUI>().text = parTotal.ToString();
        }

        void Update()
        {
            if (Time.time - scoreAnnouncementTimer > scoreAnnouncementMax)
            {
                scoreAnnouncementTimer = Mathf.Infinity;
                
                if (roundOverScreen != null)
                {
                    roundOverScreen.SetActive(true);
                    roundOverScreen.transform.Find("Hole Text").GetChild(holeIndex).GetComponent<TextMeshProUGUI>().text = player.GetSwings().ToString();
                }
            }
        }

        private string CalculateScore()
        {
            int overUnderPar = player.GetSwings() - HolePar[holeIndex];
            if (player.GetSwings() == 1)
                return "Hole in One";
            if (overUnderPar == -4)
                return "Condor";
            if (overUnderPar == -3)
                return "Albatross";
            if (overUnderPar == -2)
                return "Eagle";
            if (overUnderPar == -1)
                return "Birdie";
            if (overUnderPar == 0)
                return "Par";
            if (overUnderPar == 1)
                return "Bogie";
            if (overUnderPar == 2)
                return "Double Bogie";
            if (overUnderPar == 3)
                return "Triple Bogie";
            if (overUnderPar == 4)
                return "Quadruple Bogie";
            return "Good Job!";
        }

        // UI Button click during end of round screen popup
        public void RoundOver()
        {
            holeIndex += 1;
            if (holeIndex >= HolePar.Count)
            {
                //GameOver();
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public void Scratch()
        {
            player.AddSwing();
            player.transform.position = lastHit;
            player.ResetVelocity();
        }

        public void ResetPlayer()
        {
            player.transform.position = spawnPoint.position;
            player.ResetVelocity();
        }
    }
}