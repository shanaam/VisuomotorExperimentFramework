using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class ConfigurationBlockManager : MonoBehaviour
{
    public List<GameObject> Blocks = new List<GameObject>();
    public GameObject BlockPrefab;

    public GameObject Content;
    public GameObject SelectedBlockText;
    public GameObject InsertInstructionButton;
    public GameObject Dummy;

    public bool Dragged;

    private ExperimentContainer expContainer;
    private ConfigurationUIManager uiManager;

    public HashSet<GameObject> SelectedBlocks = new HashSet<GameObject>();
    public HashSet<GameObject> SelectedNotches = new HashSet<GameObject>();
    public HashSet<GameObject> CopiedBlocks = new HashSet<GameObject>();

    private ColorBlock selectedColourPalette, normalColourPalette;

    private const float BLOCK_SPACING = 110f;
    private const float INITIAL_OFFSET = -390f;

    private float blockViewLeftSide, blockViewRightSide;

    public void Start()
    {
        normalColourPalette.normalColor = Color.white;
        normalColourPalette.selectedColor = Color.white;
        normalColourPalette.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        normalColourPalette.pressedColor = new Color(0.78f, 0.78f, 0.78f);
        normalColourPalette.highlightedColor = new Color(0.96f, 0.96f, 0.96f);
        normalColourPalette.colorMultiplier = 1.0f;

        selectedColourPalette.normalColor = new Color(1f, 0.85f, 0.49f);
        selectedColourPalette.selectedColor = selectedColourPalette.normalColor;
        selectedColourPalette.pressedColor = Color.yellow;
        selectedColourPalette.highlightedColor = Color.yellow;
        selectedColourPalette.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        selectedColourPalette.colorMultiplier = 1.0f;

        Vector3[] corners = new Vector3[4];
        GetComponent<RectTransform>().GetWorldCorners(corners);
        blockViewLeftSide = corners[0].x;
        blockViewRightSide = corners[2].x;
    }

    public void InitializeBlockPrefabs(ConfigurationUIManager manager, ExperimentContainer expContainer)
    {
        this.expContainer = expContainer;
        this.uiManager = manager;

        for (int i = 0; i < Blocks.Count; i++)
        {
            Destroy(Blocks[i]);
        }
        
        Blocks.Clear();
        SelectedBlocks.Clear();

        List<object> per_block_type = expContainer.Data["per_block_type"] as List<object>;
        
        for (int i = 0; i < per_block_type.Count; i++)
        {
            GameObject g = Instantiate(BlockPrefab, Content.transform);
            g.name = "Block " + i;

            BlockComponent blckCmp = g.GetComponent<BlockComponent>();

            blckCmp.Block.GetComponentInChildren<Text>().text = 
                g.name + "\n" + Convert.ToString(per_block_type[i]);

            g.transform.position = new Vector3(
                INITIAL_OFFSET + (i * BLOCK_SPACING), 0f, 0f) + transform.position;

            blckCmp.BlockController = this;
            blckCmp.BlockID = i;

            Blocks.Add(g);

            blckCmp.Block.GetComponent<Button>().onClick.AddListener(
                () => { uiManager.OnClickBlock(g); });

            blckCmp.Block.GetComponent<Button>().onClick.AddListener(
                () => { OnClickBlock(g); });

            blckCmp.Notch.GetComponent<Button>().onClick.AddListener(
                () => { OnNotchPress(blckCmp.Notch); });
        }
    }

    void Update()
    {
        GetComponent<ScrollRect>().enabled = !Dragged;

        if (Dragged)
        {
            // Adjust scroll view position
            GetComponent<ScrollRect>().enabled = true;
            if (Input.mousePosition.x <= blockViewLeftSide)
            {
                GetComponent<ScrollRect>().horizontalNormalizedPosition -= 0.8f * Time.deltaTime;
            }
            else if (Input.mousePosition.x >= blockViewRightSide)
            {
                GetComponent<ScrollRect>().horizontalNormalizedPosition += 0.8f * Time.deltaTime;
            }
            GetComponent<ScrollRect>().enabled = false;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.A))
            {
                if (SelectedBlocks.Count != Blocks.Count)
                {
                    foreach (GameObject g in Blocks)
                    {
                        SelectedBlocks.Add(g);
                    }
                    SelectedNotches.Clear();

                    UpdateNotchButtons();
                    UpdateBlockButtons();
                }
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
            {
                CopyBlocks();
                Debug.LogError("copied!");
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
            {
                PasteBlocks();
                Debug.LogError("pasted!");
            }
        }
    }

    private void UpdateBlockButtons()
    {
        SelectedBlockText.GetComponent<Text>().text = "Selected Blocks: ";
        foreach (GameObject g in Blocks)
        {
            GameObject block = g.GetComponent<BlockComponent>().Block;
            if (SelectedBlocks.Contains(g))
            {
                block.GetComponent<Button>().colors = selectedColourPalette;

                SelectedBlockText.GetComponent<Text>().text +=
                    g.GetComponent<BlockComponent>().BlockID + ", ";
            }
            else
            {
                block.GetComponent<Button>().colors = normalColourPalette;
            }

        }

        
        string s = 
            SelectedBlockText.GetComponent<Text>().text.Substring(0,
                SelectedBlockText.GetComponent<Text>().text.Length - 2);

        SelectedBlockText.GetComponent<Text>().text = s;
    }

    private void UpdateNotchButtons()
    {
        if (SelectedBlocks.Count == 0)
        {
            SelectedBlockText.GetComponent<Text>().text = "Selected Blocks: None";
        }

        foreach (GameObject g in Blocks)
        {
            GameObject notch = g.GetComponent<BlockComponent>().Notch;
            if (SelectedNotches.Contains(notch))
            {
                notch.GetComponent<Button>().colors = selectedColourPalette;
            }
            else
            {
                notch.GetComponent<Button>().colors = normalColourPalette;
            }
        }
    }

    public void OnNotchPress(GameObject notch)
    {
        if (Dragged) return;

        if (SelectedNotches.Contains(notch))
        {
            SelectedNotches.Remove(notch);
        }
        else
        {
            SelectedNotches.Add(notch);
        }

        SelectedBlocks.Clear();

        InsertInstructionButton.SetActive(SelectedNotches.Count > 0);

        UpdateBlockButtons();
        UpdateNotchButtons();
    }

    public void OnClickBlock(GameObject block)
    {
        if (Dragged) return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (!SelectedBlocks.Contains(block))
            {
                SelectedBlocks.Add(block);
            }
        }
        else
        {
            SelectedBlocks.Clear();
            SelectedBlocks.Add(block);
        }

        // If a user selects a block, remove all selected notches
        SelectedNotches.Clear();

        UpdateBlockButtons();
        UpdateNotchButtons();
    }

    /// <summary>
    /// Executes ONCE on the frame the user begins dragging a block
    /// </summary>
    /// <param name="draggedObject"></param>
    public void OnBlockBeginDrag(GameObject draggedObject)
    {
        if (!SelectedBlocks.Contains(draggedObject))
        {
            // If the user drags a block they did not highlight when the
            // user has already selected a block(s), 
            if (SelectedBlocks.Count > 0)
            {
                SelectedBlocks.Clear();
            }

            SelectedBlocks.Add(draggedObject);
            UpdateBlockButtons();
        }

        // If the user selected notches, remove them from the selection
        // since we are dragging a block
        SelectedNotches.Clear();
        UpdateNotchButtons();

        // Unparent
        foreach (GameObject g in SelectedBlocks)
        {
            g.transform.SetParent(gameObject.transform);
        }

        // Enable dummy object to act as a spacer
        Dummy.SetActive(true);
    }

    /// <summary>
    /// Executes when the user clicks and drags a block
    /// </summary>
    /// <param name="draggedObject"></param>
    public void OnBlockDrag(GameObject draggedObject, Vector2 mousePosition)
    {
        // If user selected multiple blocks, also attach them to the mouse
        int j = 0;
        foreach (GameObject g in Blocks)
        {
            if (SelectedBlocks.Contains(g))
            {
                g.transform.position = 
                    new Vector3((BLOCK_SPACING * j) + mousePosition.x, mousePosition.y, 0f);
                j++;
            }
        }

        // Snaps the blocks into its correct position as the user drags the block
        // around the screen

        // Position blocks left of the cursor
        int k = Blocks.Count;
        j = 0;
        for (int i = 0; i < Blocks.Count; i++)
        {
            if (!SelectedBlocks.Contains(Blocks[i]))
            {
                if (Blocks[i].transform.position.x < mousePosition.x)
                {
                    /*
                    Blocks[i].transform.position = new Vector3(
                        INITIAL_OFFSET + (j * BLOCK_SPACING), 0f, 0f) + transform.position;
                    */
                    Blocks[i].transform.SetSiblingIndex(j);
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

        Dummy.transform.SetSiblingIndex(j);

        // Position blocks right of the cursor
        for (int i = k; i < Blocks.Count; i++)
        {
            if (!SelectedBlocks.Contains(Blocks[i]))
            {
                /*
                Blocks[i].transform.position = new Vector3(
                    INITIAL_OFFSET + (j * BLOCK_SPACING), 0f, 0f) + transform.position;
                */
                Blocks[i].transform.SetSiblingIndex(j);
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
        // Squish selected blocks to be next to each other
        int j = 1;
        foreach (GameObject g in Blocks)
        {
            if (g != draggedObject && SelectedBlocks.Contains(g))
            {
                g.transform.position = draggedObject.transform.position +
                                       new Vector3(j, 0f, 0f);
                j++;
            }

            // Reparent
            g.transform.SetParent(Content.transform);
        }

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
            BlockComponent blckCmp = Blocks[i].GetComponent<BlockComponent>();
            blckCmp.BlockID = i;
            blckCmp.Block.GetComponentInChildren<Text>().text = Blocks[i].name + "\n" + newList[i];

            Blocks[i].transform.SetSiblingIndex(i);
            /*
            Blocks[i].GetComponent<RectTransform>().position = new Vector3(
                INITIAL_OFFSET + (BLOCK_SPACING * i), 0f, 0f) + GetComponent<RectTransform>().position;
            */
        }

        uiManager.Dirty = true;
    }

    public void AddBlock()
    {
        // Instantiate prefab that represents the block in the UI
        List<object> per_block_type = expContainer.Data["per_block_type"] as List<object>;
        GameObject g = Instantiate(BlockPrefab, Content.transform);
        g.name = "Block " + per_block_type.Count;
        
        /*
        g.transform.position = new Vector3(
            INITIAL_OFFSET + (per_block_type.Count * BLOCK_SPACING), 0f, 0f) + transform.position;
        */

        g.transform.SetSiblingIndex(per_block_type.Count);

        BlockComponent blckCmp = g.GetComponent<BlockComponent>();

        blckCmp.BlockController = this;
        blckCmp.BlockID = per_block_type.Count;
        g.transform.SetAsLastSibling();

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

        blckCmp.Block.GetComponentInChildren<Text>().text = 
            g.name + "\n" + per_block_type[per_block_type.Count - 1];

        uiManager.Dirty = true;

        // Add listener for onClick function
        blckCmp.Block.GetComponent<Button>().onClick.AddListener(
            () => { uiManager.OnClickBlock(g); });

        blckCmp.Block.GetComponent<Button>().onClick.AddListener(
                () => { OnClickBlock(g); });

        blckCmp.Notch.GetComponent<Button>().onClick.AddListener(
            () => { OnNotchPress(blckCmp.Notch); });

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

            /*
            Blocks[i].transform.position = new Vector3(
                INITIAL_OFFSET + (i * BLOCK_SPACING), 0f, 0f) + transform.position;
            */

            Blocks[i].transform.SetSiblingIndex(i);

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

    public void InsertInstructions()
    {
        if (SelectedNotches.Count > 0)
        {
            foreach (GameObject notch in SelectedNotches)
            {
                // Instantiate prefab that represents the block in the UI
                List<object> per_block_type = expContainer.Data["per_block_type"] as List<object>;
                GameObject g = Instantiate(BlockPrefab, Content.transform);
                g.name = "Block " + per_block_type.Count;
                /*
                g.transform.position = new Vector3(
                                           INITIAL_OFFSET + (per_block_type.Count * BLOCK_SPACING), 0f, 0f) +
                                       transform.position;
                */
                BlockComponent blckCmp = g.GetComponent<BlockComponent>();

                blckCmp.BlockController = this;

                int insertIndex = notch.GetComponentInParent<BlockComponent>().BlockID;
                g.transform.SetSiblingIndex(insertIndex + 1);

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
                            per_block_list.Insert(insertIndex, (o as List<object>)[0]);
                        }
                        else
                        {
                            per_block_list.Insert(insertIndex, o);
                        }
                    }
                }

                //per_block_type[insertIndex] = "instruction";

                uiManager.Dirty = true;

                // Add listener for onClick function
                blckCmp.Block.GetComponent<Button>().onClick.AddListener(
                    () => { uiManager.OnClickBlock(g); });

                blckCmp.Block.GetComponent<Button>().onClick.AddListener(
                    () => { OnClickBlock(g); });

                blckCmp.Notch.GetComponent<Button>().onClick.AddListener(
                    () => { OnNotchPress(blckCmp.Notch); });

                Blocks.Insert(insertIndex + 1, g);
            }

            ResetBlockText();
        }
        else
        {
            AddBlock();
        }
        ResetBlockText();
    }

    public void ResetBlockText()
    {
        List<object> per_block_type = uiManager.ExpContainer.Data["per_block_type"] as List<object>;

        for (int i = 0; i < Blocks.Count; i++)
        {
            Blocks[i].name = "Block " + i;
            Blocks[i].GetComponent<BlockComponent>().BlockID = i;
            Blocks[i].GetComponent<BlockComponent>().Block.GetComponentInChildren<Text>().text =
                Blocks[i].name + "\n" + per_block_type[i];
        }
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

    public void CopyBlocks()
    {
        CopiedBlocks.Clear();
        CopiedBlocks = new HashSet<GameObject>(SelectedBlocks, SelectedBlocks.Comparer);
    }

    public void PasteBlocks()
    {
        if (SelectedNotches.Count == 1)
        {
            IEnumerable<GameObject> query = CopiedBlocks.OrderBy(pet => pet.name);

            GameObject notch = null;

            foreach (GameObject notches in SelectedNotches)
            {
                notch = notches;
            }

            int count = 0;
            foreach (GameObject copiedBlock in query)
            {

                // Instantiate prefab that represents the block in the UI
                List<object> per_block_type = expContainer.Data["per_block_type"] as List<object>;
                GameObject g = Instantiate(BlockPrefab, Content.transform);
                g.name = "Block " + per_block_type.Count;

                BlockComponent blckCmp = g.GetComponent<BlockComponent>();

                blckCmp.BlockController = this;

                int insertIndex = notch.GetComponentInParent<BlockComponent>().BlockID + count;
                g.transform.SetSiblingIndex(insertIndex + 2);

                // Note: We set block ID before adding another block to the dictionary because
                // block ID is zero based and the count will be 1 off after the GameObject
                // is set up.
                foreach (KeyValuePair<string, object> kp in expContainer.Data)
                {
                    if (kp.Key.StartsWith("per_block"))
                    {
                        List<object> per_block_list = expContainer.Data[kp.Key] as List<object>;
                        //object o = expContainer.GetDefaultValue(kp.Key);
                        //copiedBlock.GetComponent<BlockComponent>()

                        object o = (per_block_list[copiedBlock.GetComponent<BlockComponent>().BlockID]);

                        // The default value of 
                        if (o is IList && o.GetType().IsGenericType &&
                            o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
                        {
                            per_block_list.Insert(insertIndex + 1, (o as List<object>)[0]);
                        }
                        else
                        {
                            per_block_list.Insert(insertIndex + 1, o);
                        }
                    }
                }

                uiManager.Dirty = true;

                // Add listener for onClick function
                blckCmp.Block.GetComponent<Button>().onClick.AddListener(
                    () => { uiManager.OnClickBlock(g); });

                blckCmp.Block.GetComponent<Button>().onClick.AddListener(
                    () => { OnClickBlock(g); });

                blckCmp.Notch.GetComponent<Button>().onClick.AddListener(
                    () => { OnNotchPress(blckCmp.Notch); });

                Blocks.Insert(insertIndex + 1, g);


                var newList = expContainer.Data["per_block_type"] as List<object>;

                // Fix numbering for the new block orientation
                for (int i = 0; i < Blocks.Count; i++)
                {
                    Blocks[i].name = "Block " + i;
                    BlockComponent blockCmp = Blocks[i].GetComponent<BlockComponent>();
                    blockCmp.BlockID = i;
                    blockCmp.Block.GetComponentInChildren<Text>().text = Blocks[i].name + "\n" + newList[i];

                    Blocks[i].transform.SetSiblingIndex(i);
                    /*
                    Blocks[i].GetComponent<RectTransform>().position = new Vector3(
                        INITIAL_OFFSET + (BLOCK_SPACING * i), 0f, 0f) + GetComponent<RectTransform>().position;
                    */
                }

                count++;
            }

            ResetBlockText();
        }
    }
}
