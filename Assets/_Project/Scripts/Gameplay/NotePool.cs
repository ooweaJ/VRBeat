using System.Collections.Generic;
using UnityEngine;

public class NotePool : MonoBehaviour
{
    [SerializeField] NoteBase normalNotePrefab;
    [SerializeField] NoteBase longNotePrefab;

    readonly Queue<NoteBase> normalPool = new Queue<NoteBase>();
    readonly Queue<NoteBase> longPool   = new Queue<NoteBase>();

    public void Warmup(int count)
    {
        int half = count / 2;
        for (int i = 0; i < half; i++)
        {
            Enqueue(Create(normalNotePrefab), normalPool);
            Enqueue(Create(longNotePrefab),   longPool);
        }
    }

    NoteBase Create(NoteBase prefab)
    {
        var note = Instantiate(prefab, transform);
        note.gameObject.SetActive(false);
        return note;
    }

    void Enqueue(NoteBase note, Queue<NoteBase> pool) => pool.Enqueue(note);

    public NoteBase Get(string type)
    {
        bool isLong = type == "long";
        var pool   = isLong ? longPool   : normalPool;
        var prefab = isLong ? longNotePrefab : normalNotePrefab;

        if (pool.Count == 0)
            Enqueue(Create(prefab), pool);

        var note = pool.Dequeue();
        note.gameObject.SetActive(true);
        return note;
    }

    public void Return(NoteBase note)
    {
        note.gameObject.SetActive(false);
        note.transform.SetParent(transform);

        if (note is LongNote)
            longPool.Enqueue(note);
        else
            normalPool.Enqueue(note);
    }
}
