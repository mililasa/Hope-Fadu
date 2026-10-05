using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class nubeMovimiento : MonoBehaviour
{
    // Start is called before the first frame update
    //Separador de 'Movimiento'

    [Header("nubeMovimiento")]

    //Con esto vas a poder ajustar parámetros de velocidad, spawnpoint, endpoint y startpoint de cada nube
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float speed = 1f;

    private bool comienzaMovimiento = true; // para poder empezar cada nube en distintos lugares

    // para que sea transparente

    [Header("Apariencia")]
    [SerializeField, Range(0f, 1f)] private float opacidad = 1f;

    void Start()
    {
        transform.position = spawnPoint.position;
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color color = spriteRenderer.color;
        color.a = opacidad;
        spriteRenderer.color = color;

    }

    // Update is called once per frame
    void Update()
    {
        if (comienzaMovimiento) {

        transform.position = Vector3.MoveTowards(transform.position, endPoint.position, speed * Time.deltaTime);
        };

            if (transform.position == endPoint.position)
            {
                comienzaMovimiento = false;
            }

        else
        {
            transform.position = Vector3.MoveTowards(transform.position, endPoint.position, speed * Time.deltaTime);
        };
        
        if (transform.position == endPoint.position)
        {
            transform.position = startPoint.position;
        }

    }
}
