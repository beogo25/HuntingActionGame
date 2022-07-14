using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WarehouseUI : MonoBehaviour
{
    public ItemInformation itemInformation;
    public int selectSlot = 0;
    public WarehouseSlot[] slots;
    public int SelectSlot
    {
        get { return selectSlot; }
        set
        {
            selectSlot = value;
            //uiǥ�����ֱ�
        }
    }
    private void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].num = i;
    }
    private void OnValidate()
    {
        slots = transform.GetComponentsInChildren<WarehouseSlot>();
    }
    private void OnEnable()
    {
        Refresh();
    }
    public virtual void ItemInformationChange(int num)
    {
        itemInformation.WareHouseBool = true;
        itemInformation.targetNum = num;
    }
    public abstract void Refresh();
}