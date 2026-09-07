using UnityEngine;

public interface IDanificavel
{
    void ReceberDano(float quantidade, GameObject origem = null);
    bool EstaMorto { get; }
}