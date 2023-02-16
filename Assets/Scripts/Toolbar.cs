using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    public RectTransform highlight;
    public int slotIndex = 0;

    private void Start()
    {
        byte index = 1;
        foreach (var s in slots)
        {
            var stack = new ItemStack(index, Random.Range(2, 65));
            var slot = new ItemSlot(s, stack);
            index++;
        }
    }
}
