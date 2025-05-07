using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeltInventory : MonoBehaviour
{
    public Inventory inventory;
    public WeaponsInventory weaponsInventory;
    public ClothesInventory clothesInventory;
    public GameObject[] slots;  // Inventory slots to hold GameObjects
    private int currentWeaponIndex = -1; // Index of currently equipped weapon (-1 means none)
    public playerController playerController;

    private void Start()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = null; // Initialize slots to be empty
        }
    }

    private void Update()
    {
        // Handle number key inputs (1-8) to select inventory slot
        for (int i = 0; i < slots.Length; i++)
        {
            if (Input.GetKeyDown((KeyCode)(i + KeyCode.Alpha1)))  // Keys 1-8
            {
                EquipWeapon(i);
            }
        }

        // Drop item logic (press 'D' to drop selected item)
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropSelectedItem();
        }
    }

    // Select a weapon from a slot


    // Add an item to the first available slot
    
    public void AddItemToSlot(Collectable itemPrefab)
    {
        if (itemPrefab.item.ItemType() == "Item")
        {
            Debug.Log(itemPrefab.item);
            inventory.AddItem(itemPrefab.item.GiveName(),itemPrefab.quantity);
            return;
        }    
        
        if (itemPrefab.item.ItemType() == "Weapon") 
        { 
        
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) // Find the first empty slot
                {

                    slots[i] = itemPrefab.inplayerprefab;
                    slots[i].SetActive(false); // Keep it hidden until equipped

                    Debug.Log("Added item to slot: " + i);
                    return;
                }
                
            }
            Debug.Log("Inventory full! Cannot add item.");
            weaponsInventory.AddItem(itemPrefab.item.GiveName(), itemPrefab.quantity);
        }
        if (itemPrefab.item.ItemType() == "Clothe")
        {
            clothesInventory.AddItem(itemPrefab.item.GiveName(), itemPrefab.quantity);
            return;
        }

    }

    // Drop the selected item
    public void DropSelectedItem()
    {
        if (currentWeaponIndex >= 0 && slots[currentWeaponIndex] != null)
        {
            GameObject itemToDrop = slots[currentWeaponIndex];

            // Create a clone of the item
            GameObject clone = Instantiate(itemToDrop);

            // Remove from inventory (disable the original item)
            slots[currentWeaponIndex] = null;
            currentWeaponIndex = -1;
            itemToDrop.SetActive(false); // Deactivate the original item

            // Set the position of the clone in front of the player
            clone.transform.position = transform.position + transform.forward;

            // Add Rigidbody for physics drop effect
            Rigidbody rb = clone.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = clone.AddComponent<Rigidbody>();
            }
            rb.AddForce(transform.forward * 5f, ForceMode.Impulse);

            Debug.Log("Dropped item: " + clone.name);
        }
        else
        {
            Debug.Log("No item to drop.");
        }
    }


    // Equip a weapon in the selected slot
    void EquipWeapon(int index)
    {
        
        if (index < 0 || index >= slots.Length || slots[index] == null)
        {
            Debug.Log("No weapon at this index.");
            return;
        }

        foreach (GameObject weapon in slots)
        {
            if (weapon != null)
            {
                WeaponBase wip = weapon.GetComponent<WeaponBase>();
                if (wip != null)
                {
                    wip.gameObject.SetActive(false);
                }
               
            }
                
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                WeaponBase weapon = slots[i].GetComponent<WeaponBase>();
                if (weapon != null)
                {
                    weapon.gameObject.SetActive(false);
                }
            }
        }

        WeaponBase wp = slots[index].GetComponent<WeaponBase>();
        // Enable selected weapon
        wp.gameObject.SetActive(true);
       
        currentWeaponIndex = index;
        Debug.Log($"Equipped weapon: {slots[index].name}");
        if (playerController != null)
        {
            playerController.SetCurrentWeapon(wp.gameObject);  // Pass the selected weapon to PlayerController
        }
        
    }
   

}
