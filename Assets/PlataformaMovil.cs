using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]


public class PlataformaMovil : MonoBehaviour

{
    // creo que es para elegir la plataforma
    public GameObject platform;

    // y ahora para tener parámetro de velocidad:
    public float moveSpeed;

    public Transform currentPoint; 

    public Transform[] points;

    public int pointSelection;

    private Rigidbody2D rb;

    void Start ()
    {
        rb = GetComponent<Rigidbody2D>();
        currentPoint = points[pointSelection];
    }

    void Update()
    {
        Vector2 newPosition = Vector2.MoveTowards(rb.position, currentPoint.position, Time.deltaTime * moveSpeed);

    

        rb.MovePosition(newPosition);


        if (Vector2.Distance(rb.position, currentPoint.position) < 0.01f)
        {

            pointSelection++;

            if (pointSelection >= points.Length)
            {
                pointSelection = 0;
                
            }
            

            currentPoint = points[pointSelection];
        }
    }
}