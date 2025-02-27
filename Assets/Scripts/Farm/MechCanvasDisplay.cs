using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/*
 * Control a single chromosome on the farm
 * Carry attributes of a mech
 */
public class MechCanvasDisplay : MonoBehaviour
{
    // Chromosome of this mech
    public MechChromo MyMechSO;
    public MechPresetSO MyPresetSO;

    // head, body-line, body-color, acc
    [SerializeField] public Image[] myRenderer;

    // Set sprites to match chromo
    public virtual void SetChromo(MechChromo c)
    {
        if (c == null)
        {
            MyMechSO = null;
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            MyMechSO = c;
            myRenderer[0].sprite = Resources.Load<Sprite>
                (Path.Combine("Sprites", "Mech", "Heads", "Head" + (c.Head + 1)));
            myRenderer[2].color = new Color32
                ((byte)MyMechSO.Body[0], (byte)MyMechSO.Body[1], (byte)MyMechSO.Body[2], 255);
            myRenderer[3].sprite = Resources.Load<Sprite>
                (Path.Combine("Sprites", "Mech", "Accs", "Plus" + (c.Acc + 1)));
        }
    }

    public void SetChromoFromPreset(MechPresetSO c)
    {
        if (MyMechSO == null && c != null)
        {
            gameObject.SetActive(true);
            MyPresetSO = c;
            myRenderer[0].sprite = Resources.Load<Sprite>
                (Path.Combine("Sprites", "Mech", "Heads", "Head" + (c.Head + 1)));
            myRenderer[2].color = new Color32
                ((byte)c.Body[0], (byte)c.Body[1], (byte)c.Body[2], 255);
            myRenderer[3].sprite = Resources.Load<Sprite>
                (Path.Combine("Sprites", "Mech", "Accs", "Plus" + (c.Acc + 1)));
        }
    }
}
