using TMPro;
using UnityEngine;

namespace FastStudios.Demo
{
    public class TestConditions : MonoBehaviour
    {
        public TMP_Text text;
        public int Counter;
        public bool hasSecondCounter;
        public int SecondCounter;

        void Update()
        {
            if (hasSecondCounter)
            {
                if (text.text != $"Left: {Counter}\nRight: {SecondCounter}")
                {
                    text.text = $"Left: {Counter}\nRight: {SecondCounter}";
                }
            }
            else
            {
                if (text.text != Counter.ToString())
                {
                    text.text = Counter.ToString();
                }
            }
        }

        public void IncreaseCount()
        {
            Counter++;
        }

        public void IncreaseSecondCounter()
        {
            SecondCounter++;
        }

    }

}