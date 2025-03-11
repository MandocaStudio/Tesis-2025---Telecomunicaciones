using UnityEngine;

public class test : MonoBehaviour
{
public bool conectado;
public bool foco_quemado;

private void Start() {
    if (conectado == false ){
    //enchufala webon
    Debug.Log("debes enchufarla");
    }
else if(foco_quemado == true){
    //cambiar foco
        Debug.Log("debes cambiar foco");
}
else if(conectado == true && foco_quemado == false){
    //comprar nueva lampara
        Debug.Log("debes comprar nueva lampara");

}
}
private void Update(){

}
}
