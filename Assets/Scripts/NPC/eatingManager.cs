using System.Collections;
using UnityEngine;
using DG.Tweening;

public class eatingManager : MonoBehaviour
{
    public enum Foods
    {
        noodle,
        tuna,
    }

    bool drinking = false;

    [SerializeField] ManageIK ik;

    [SerializeField] Transform chopsticks;
    [SerializeField] Transform chopsticksTarget;

    [SerializeField] Transform mouth;
    [SerializeField] Transform food;

    [SerializeField] Transform cup;
    [SerializeField] Transform cupTarget;
    [SerializeField] Transform cupResting;
    [SerializeField] Transform cupMouth;
    [SerializeField] Transform cupDrink;

    [SerializeField] float foodDuration = 2f;
    [SerializeField] float drinkDuration = 2f;



    [SerializeField] GameObject[] foodItems;
    [SerializeField] Foods currentFood;

    Mouth mouthScript;

    void Start()
    {
        mouthScript = ik.gameObject.GetComponent<Mouth>();
        foreach (GameObject food in foodItems)
            food.SetActive(false);
        chopsticks.position = food.position;
        chopsticks.rotation = food.rotation;

        StartCoroutine(Eat());
    }

    IEnumerator Eat()
    {
        int times = 0;
        while(true)
        {
            if (times < 9)
            {
                ik.SetRightHand(chopsticksTarget);
                float randomDuration = Random.Range(0.05f, 0.13f);
                foodItems[(int)currentFood].SetActive(true);
                yield return new WaitForSeconds(1);
                chopsticks.DOMove(mouth.position, foodDuration);
                chopsticks.DORotateQuaternion(mouth.rotation, foodDuration);
                yield return new WaitForSeconds(foodDuration + randomDuration);
                mouthScript.Eat();
                foodItems[(int)currentFood].SetActive(false);
                yield return new WaitForSeconds(1f);
                chopsticks.DOMove(food.position, foodDuration);
                chopsticks.DORotateQuaternion(food.rotation, foodDuration);
                yield return new WaitForSeconds(foodDuration + randomDuration);
            }
            else
            {
                ik.SetRightHand(cupTarget);
                float randomDuration = Random.Range(0.05f, 0.13f);
                yield return new WaitForSeconds(1);
                cup.DOMove(cupMouth.position, drinkDuration);
                cup.DORotateQuaternion(cupMouth.rotation, drinkDuration);
                yield return new WaitForSeconds(foodDuration + randomDuration);
                mouthScript.Drink(drinkDuration);
                cup.DOMove(cupDrink.position, drinkDuration);
                cup.DORotateQuaternion(cupDrink.rotation, drinkDuration);
                yield return new WaitForSeconds(1f);
                cup.DOMove(cupResting.position, drinkDuration);
                cup.DORotateQuaternion(cupResting.rotation, drinkDuration);
                yield return new WaitForSeconds(drinkDuration + randomDuration);
                times = 0;
            }
            times += Random.Range(1, 4);

        }
    }
}
