using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class UIBlock : MonoBehaviour
{
    public List<GameObject> Blocks = new List<GameObject>();
    public GameObject BlockPrefab;


    public GameObject Content;

    public bool Dragged;

    private ExperimentContainer expContainer;
    private ConfigurationUIManager uiManager;

    public void InitializeBlockPrefabs(ConfigurationUIManager manager, ExperimentContainer expContainer)
    {
        this.expContainer = expContainer;
        this.uiManager = manager;

        for (int i = 0; i < Blocks.Count; i++)
        {
            Destroy(Blocks[i]);
        }
        
        Blocks.Clear();

        List<object> per_block_type = expContainer.Data["per_block_type"] as List<object>;
        
        for (int i = 0; i < per_block_type.Count; i++)
        {
            GameObject g = Instantiate(BlockPrefab, Content.transform);
            g.name = "Block " + i;
            g.GetComponentInChildren<Text>().text = 
                g.name + "\n" + Convert.ToString(per_block_type[i]);

            g.transform.position = new Vector3(
                -390 + (i * 100), 0f, 0f) + transform.position;

            g.GetComponent<BlockComponent>().BlockController = this;

            g.GetComponent<BlockComponent>().BlockID = i;

            Blocks.Add(g);

            g.GetComponent<Button>().onClick.AddListener(() =>
            {
                uiManager.OnClickBlock(g);
            });
        }
    }

    void Update()
    {
        GetComponent<ScrollRect>().enabled = !Dragged;
    }

    public void OnBlockBeginDrag(GameObject draggedObject)
    {
        
    }

    /// <summary>
    /// Executes when the user clicks and drags a block
    /// </summary>
    /// <param name="draggedObject"></param>
    public void OnBlockDrag(GameObject draggedObject)
    {
        // Snaps the blocks into its correct position as the user drags the block
        // around the screen

        int j = 0, k = Blocks.Count;
        for (int i = 0; i < Blocks.Count; i++)
        {
            if (Blocks[i] != draggedObject)
            {
                if (Blocks[i].transform.position.x < draggedObject.transform.position.x)
                {
                    Blocks[i].transform.position = new Vector3(
                        -390f + (j * 100f), 0f, 0f) + transform.position;
                    j++;
                }
                else
                {
                    k = i;
                    j++;
                    break;
                }
            }
        }

        for (int i = k; i < Blocks.Count; i++)
        {
            if (Blocks[i] != draggedObject)
            {
                Blocks[i].transform.position = new Vector3(
                    -390f + (j * 100f), 0f, 0f) + transform.position;
                j++;
            }
        }
    }

    /// <summary>
    /// Executes when the user lets go of a block
    /// </summary>
    /// <param name="draggedObject"></param>
    public void OnEndDrag(GameObject draggedObject)
    {
        // Reorganize the blocks based on their x coordinate
        Blocks.Sort((a, b) =>
            a.GetComponent<RectTransform>().position.x.CompareTo(
            b.GetComponent<RectTransform>().position.x));

        // Reorganize per_block list
        var keys = expContainer.Data.Keys.ToList();
        foreach (string key in keys)
        {
            if (key.StartsWith("per_block"))
            {
                List<object> tempList = new List<object>();
                var oldList = expContainer.Data[key] as List<object>;
                for (int i = 0; i < Blocks.Count; i++)
                {
                    tempList.Add(oldList[Blocks[i].GetComponent<BlockComponent>().BlockID]);
                }

                expContainer.Data[key] = tempList;
            }
        }

        var newList = expContainer.Data["per_block_type"] as List<object>;

        // Fix numbering for the new block orientation
        for (int i = 0; i < Blocks.Count; i++)
        {
            Blocks[i].name = "Block " + i;
            Blocks[i].GetComponent<BlockComponent>().BlockID = i;
            Blocks[i].GetComponentInChildren<Text>().text = Blocks[i].name + "\n" + newList[i];
            Blocks[i].GetComponent<RectTransform>().position = new Vector3(
                -390 + (100 * i), 0f, 0f) + GetComponent<RectTransform>().position;
        }

        uiManager.Dirty = true;
    }

    public void AddBlock()
    {
        // Instantiate prefab that represents the block in the UI
        List<object> per_block_type = expContainer.Data["per_block_type"] as List<object>;
        GameObject g = Instantiate(BlockPrefab, Content.transform);
        g.name = "Block " + per_block_type.Count;
        
        g.transform.position = new Vector3(
            -390 + (per_block_type.Count * 100), 0f, 0f) + transform.position;

        g.GetComponent<BlockComponent>().BlockController = this;
        g.GetComponent<BlockComponent>().BlockID = per_block_type.Count;

        // Note: We set block ID before adding another block to the dictionary because
        // block ID is zero based and the count will be 1 off after the GameObject
        // is set up.
        foreach (KeyValuePair<string, object> kp in expContainer.Data)
        {
            if (kp.Key.StartsWith("per_block"))
            {
                List<object> per_block_list = expContainer.Data[kp.Key] as List<object>;
                object o = expContainer.GetDefaultValue(kp.Key);

                // The default value of 
                if (o is IList && o.GetType().IsGenericType &&
                    o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
                {
                    per_block_list.Add((o as List<object>)[0]);
                }
                else
                {
                    per_block_list.Add(o);
                }
            }
        }

        g.GetComponentInChildren<Text>().text = g.name + "\n" + per_block_type[per_block_type.Count - 1];

        uiManager.Dirty = true;

        // Add listener for onClick function
        g.GetComponent<Button>().onClick.AddListener(
            () => { uiManager.OnClickBlock(g); });

        Blocks.Add(g);

        uiManager.BlockPanel.GetComponent<BlockPanel>().Populate(per_block_type.Count - 1);
    }

    /// <summary>
    /// Executes when the remove button is pressed. The currently selected block is deleted
    /// </summary>
    public void RemoveBlock()
    {
        // Remove the specific block from each per_block_ parameter
        var keys = expContainer.Data.Keys.ToList();

        foreach (string key in keys)
        {
            if (key.StartsWith("per_block"))
            {
                List<object> per_block_list = expContainer.Data[key] as List<object>;

                if (per_block_list.Count == 0)
                {
                    return;
                }

                per_block_list.RemoveAt(uiManager.CurrentSelectedBlock);
            }
        }

        // Remove the visual representation of the block
        GameObject g = Blocks[uiManager.CurrentSelectedBlock];
        Blocks.RemoveAt(uiManager.CurrentSelectedBlock);
        Destroy(g);

        // Every block to the right of the block that was removed must be readjusted as
        // their indexes are no longer correct
        List<object> per_block_type = expContainer.Data["per_block_type"] as List<object>;
        for (int i = uiManager.CurrentSelectedBlock; i < Blocks.Count; i++)
        {
            Blocks[i].name = "Block " + i;

            Blocks[i].GetComponentInChildren<Text>().text =
                Blocks[i].name + "\n" + Convert.ToString(per_block_type[i]);

            Blocks[i].transform.position = new Vector3(
                -390 + (i * 100), 0f, 0f) + transform.position;

            Blocks[i].GetComponent<BlockComponent>().BlockID = i;
        }

        // Loads the currently selected block
        if (Blocks.Count > 0)
        {
            uiManager.CurrentSelectedBlock = Math.Max(
                uiManager.CurrentSelectedBlock - 1, 0
            );
            uiManager.BlockPanel.GetComponent<BlockPanel>().Populate(uiManager.CurrentSelectedBlock);
        }
        else
        {
            // If there are no more blocks, then disable the ability to edit them
            foreach (Transform child in uiManager.BlockPanel.transform)
            {
                child.gameObject.SetActive(false);
            }
        }

        uiManager.Dirty = true;
    }

    /// <summary>
    /// Updates the text display for the currently selected block if
    /// per_block_type is changed
    /// </summary>
    public void UpdateBlockText()
    {
        List<object> per_block_type = uiManager.ExpContainer.Data["per_block_type"] as List<object>;

        Blocks[uiManager.CurrentSelectedBlock].GetComponentInChildren<Text>().text =
            Blocks[uiManager.CurrentSelectedBlock].name + "\n" +
            Convert.ToString(per_block_type[uiManager.CurrentSelectedBlock]);
    }
}
