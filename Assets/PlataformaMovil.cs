using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Numerics;

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

    void Start ()
    {
        currentPoint = points[pointSelection];
    }

    void Update ()
    {
        platform.transform.position = UnityEngine.Vector3.MoveTowards(platform.transform.position, currentPoint.position, Time.deltaTime * moveSpeed);
    }
}