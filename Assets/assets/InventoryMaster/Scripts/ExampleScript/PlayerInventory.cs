using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerInventory : MonoBehaviour
{
    public GameObject inventory;
    public GameObject characterSystem;
    public GameObject craftSystem;
    private Inventory craftSystemInventory;
    private CraftSystem cS;
    private Inventory mainInventory;
    private Inventory characterSystemInventory;
    private Tooltip toolTip;

    private InputManager inputManagerDatabase;

    Slider hpSlider;
    Slider hungrySlider;
    Slider thirstySlider;

    public float maxHealth = 100;
    public float maxHungry = 100;
    public float maxThirsty = 100;
    float maxDamage = 0;
    float maxArmor = 0;

    public float diminutionHungryStep = 1f;
    public float hungryDamageStep = 2f;
    public float hungryTreatStep = 1f;
    public float hungryLimitDamage = 15;
    public float hungryLimitTreat = 70;
    public float diminutionThirstyStep = 1f;
    public float thirstyDamageStep = 2f;
    public float thirstyTreatStep = 1f;
    public float thirstyLimitDamage = 15;
    public float thirstyLimitTreat = 70;
    float currentHealth = 100;
    float currentHungry = 100;
    float currentThirsty = 100;
    float currentDamage = 0;
    float currentArmor = 0;

    int normalSize = 3;

    public void OnEnable()
    {
        Inventory.ItemEquip += OnBackpack;
        Inventory.UnEquipItem += UnEquipBackpack;

        Inventory.ItemEquip += OnGearItem;
        Inventory.ItemConsumed += OnConsumeItem;
        Inventory.UnEquipItem += OnUnEquipItem;

        Inventory.ItemEquip += EquipWeapon;
        Inventory.UnEquipItem += UnEquipWeapon;
    }

    public void OnDisable()
    {
        Inventory.ItemEquip -= OnBackpack;
        Inventory.UnEquipItem -= UnEquipBackpack;

        Inventory.ItemEquip -= OnGearItem;
        Inventory.ItemConsumed -= OnConsumeItem;
        Inventory.UnEquipItem -= OnUnEquipItem;

        Inventory.UnEquipItem -= UnEquipWeapon;
        Inventory.ItemEquip -= EquipWeapon;
    }

    void EquipWeapon(Item item)
    {
        if (item.itemType == ItemType.Weapon)
        {
            //add the weapon if you unequip the weapon
        }
    }

    void UnEquipWeapon(Item item)
    {
        if (item.itemType == ItemType.Weapon)
        {
            //delete the weapon if you unequip the weapon
        }
    }

    void OnBackpack(Item item)
    {
        if (item.itemType == ItemType.Backpack)
        {
            for (int i = 0; i < item.itemAttributes.Count; i++)
            {
                if (mainInventory == null)
                    mainInventory = inventory.GetComponent<Inventory>();
                mainInventory.sortItems();
                if (item.itemAttributes[i].attributeName == "Slots")
                    changeInventorySize(item.itemAttributes[i].attributeValue);
            }
        }
    }

    void UnEquipBackpack(Item item)
    {
        if (item.itemType == ItemType.Backpack)
            changeInventorySize(normalSize);
    }

    void changeInventorySize(int size)
    {
        dropTheRestItems(size);

        if (mainInventory == null)
            mainInventory = inventory.GetComponent<Inventory>();
        if (size == 3)
        {
            mainInventory.width = 3;
            mainInventory.height = 1;
            mainInventory.updateSlotAmount();
            mainInventory.adjustInventorySize();
        }
        if (size == 6)
        {
            mainInventory.width = 3;
            mainInventory.height = 2;
            mainInventory.updateSlotAmount();
            mainInventory.adjustInventorySize();
        }
        else if (size == 12)
        {
            mainInventory.width = 4;
            mainInventory.height = 3;
            mainInventory.updateSlotAmount();
            mainInventory.adjustInventorySize();
        }
        else if (size == 16)
        {
            mainInventory.width = 4;
            mainInventory.height = 4;
            mainInventory.updateSlotAmount();
            mainInventory.adjustInventorySize();
        }
        else if (size == 24)
        {
            mainInventory.width = 6;
            mainInventory.height = 4;
            mainInventory.updateSlotAmount();
            mainInventory.adjustInventorySize();
        }
    }

    void dropTheRestItems(int size)
    {
        if (size < mainInventory.ItemsInInventory.Count)
        {
            for (int i = size; i < mainInventory.ItemsInInventory.Count; i++)
            {
                GameObject dropItem = (GameObject)Instantiate(mainInventory.ItemsInInventory[i].itemModel);
                dropItem.AddComponent<PickUpItem>();
                dropItem.GetComponent<PickUpItem>().item = mainInventory.ItemsInInventory[i];
                dropItem.transform.localPosition = GameObject.FindGameObjectWithTag("Player").transform.localPosition;
            }
        }
    }

    void Start()
    {
        hpSlider = GameObject.Find("SliderHP").GetComponent<Slider>();
        hungrySlider = GameObject.Find("SliderHungry").GetComponent<Slider>();
        thirstySlider = GameObject.Find("SliderThirsty").GetComponent<Slider>();


        if (inputManagerDatabase == null)
            inputManagerDatabase = (InputManager)Resources.Load("InputManager");

        if (craftSystem != null)
            cS = craftSystem.GetComponent<CraftSystem>();

        if (GameObject.FindGameObjectWithTag("Tooltip") != null)
            toolTip = GameObject.FindGameObjectWithTag("Tooltip").GetComponent<Tooltip>();
        if (inventory != null)
            mainInventory = inventory.GetComponent<Inventory>();
        if (characterSystem != null)
            characterSystemInventory = characterSystem.GetComponent<Inventory>();
        if (craftSystem != null)
            craftSystemInventory = craftSystem.GetComponent<Inventory>();
    }


    public void OnConsumeItem(Item item)
    {
        for (int i = 0; i < item.itemAttributes.Count; i++)
        {
            if (item.itemAttributes[i].attributeName == "Health")
            {
                currentHealth = Mathf.Min(currentHealth + item.itemAttributes[i].attributeValue, maxHealth);
            }
            if (item.itemAttributes[i].attributeName == "Hungry")
            {
                currentHungry = Mathf.Min(currentHungry + item.itemAttributes[i].attributeValue, maxHungry);
            }
            if (item.itemAttributes[i].attributeName == "Thirsty")
            {
                Debug.Log("drink");
                currentThirsty = Mathf.Min(currentThirsty + item.itemAttributes[i].attributeValue, maxThirsty);
            }
            if (item.itemAttributes[i].attributeName == "Armor")
            {
                currentArmor = Mathf.Min(currentArmor + item.itemAttributes[i].attributeValue, maxArmor);
            }
            if (item.itemAttributes[i].attributeName == "Damage")
            {
                currentDamage = Mathf.Min(currentDamage + item.itemAttributes[i].attributeValue, maxDamage);
            }            
            if (item.itemAttributes[i].attributeName == "Drop")
            {
                Debug.Log("test");
                bool check = characterSystemInventory.checkIfItemAllreadyExist(item.itemAttributes[i].attributeValue, 1);
                if (!check && characterSystemInventory.ItemsInInventory.Count < (characterSystemInventory.width * characterSystemInventory.height))
                {
                    Debug.Log("test1");
                    characterSystemInventory.addItemToInventory(item.itemAttributes[i].attributeValue, 1);
                    characterSystemInventory.updateItemList();
                    characterSystemInventory.stackableSettings();
                }
            }
        }
    }

    public void OnGearItem(Item item)
    {
        for (int i = 0; i < item.itemAttributes.Count; i++)
        {
            if (item.itemAttributes[i].attributeName == "Health")
                maxHealth += item.itemAttributes[i].attributeValue;
            if (item.itemAttributes[i].attributeName == "Hungry")
                maxHungry += item.itemAttributes[i].attributeValue;
            if (item.itemAttributes[i].attributeName == "Armor")
                maxArmor += item.itemAttributes[i].attributeValue;
            if (item.itemAttributes[i].attributeName == "Damage")
                maxDamage += item.itemAttributes[i].attributeValue;
        }
    }

    public void OnUnEquipItem(Item item)
    {
        for (int i = 0; i < item.itemAttributes.Count; i++)
        {
            if (item.itemAttributes[i].attributeName == "Health")
                maxHealth -= item.itemAttributes[i].attributeValue;
            if (item.itemAttributes[i].attributeName == "Hungry")
                maxHungry -= item.itemAttributes[i].attributeValue;
            if (item.itemAttributes[i].attributeName == "Armor")
                maxArmor -= item.itemAttributes[i].attributeValue;
            if (item.itemAttributes[i].attributeName == "Damage")
                maxDamage -= item.itemAttributes[i].attributeValue;
        }
    }



    // Update is called once per frame
    void Update()
    {
        currentHungry -= Mathf.Max(0f, diminutionHungryStep * Time.deltaTime);
        hungrySlider.value = currentHungry / maxHungry;
        currentThirsty -= Mathf.Max(0f, diminutionThirstyStep * Time.deltaTime);
        thirstySlider.value = currentThirsty / maxThirsty;

        if(currentHungry < hungryLimitDamage)
            currentHealth -= Mathf.Max(0f, hungryDamageStep * Time.deltaTime);
        if(currentHungry > hungryLimitTreat)
            currentHealth += Mathf.Min(maxHealth, hungryTreatStep * Time.deltaTime);

        if(currentThirsty < thirstyLimitDamage)
            currentHealth -= Mathf.Max(0f, thirstyDamageStep * Time.deltaTime);
        if(currentThirsty > thirstyLimitTreat)
            currentHealth += Mathf.Min(maxHealth, thirstyTreatStep * Time.deltaTime);

        hpSlider.value = currentHealth / maxHealth;

        if (Input.GetKeyDown(inputManagerDatabase.CharacterSystemKeyCode))
        {
            if (!characterSystem.activeSelf)
            {
                characterSystemInventory.openInventory();
            }
            else
            {
                if (toolTip != null)
                    toolTip.deactivateTooltip();
                characterSystemInventory.closeInventory();
            }
        }

        if (Input.GetKeyDown(inputManagerDatabase.InventoryKeyCode))
        {
            if (!inventory.activeSelf)
            {
                mainInventory.openInventory();
            }
            else
            {
                if (toolTip != null)
                    toolTip.deactivateTooltip();
                mainInventory.closeInventory();
            }
        }

        if (Input.GetKeyDown(inputManagerDatabase.CraftSystemKeyCode))
        {
            if (!craftSystem.activeSelf)
                craftSystemInventory.openInventory();
            else
            {
                if (cS != null)
                    cS.backToInventory();
                if (toolTip != null)
                    toolTip.deactivateTooltip();
                craftSystemInventory.closeInventory();
            }
        }

    }

}
