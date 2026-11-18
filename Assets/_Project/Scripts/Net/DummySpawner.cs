using UnityEngine;
using Mirror;

public class DummySpawner : NetworkBehaviour
{
    [SerializeField] GameObject _dummyPrefab;
    [SerializeField] Vector3 _spawnPosition = new Vector3(0f, 1f, 8f);
    [SerializeField] int _count = 1;

    public override void OnStartServer()
    {
        if (_dummyPrefab == null) return;

        for (int i = 0; i < _count; i++)
        {
            Vector3 pos = _spawnPosition + new Vector3(i * 2.5f, 0f, 0f);
            GameObject dummy = Instantiate(_dummyPrefab, pos, Quaternion.identity);
            NetworkServer.Spawn(dummy);
        }
    }
}
