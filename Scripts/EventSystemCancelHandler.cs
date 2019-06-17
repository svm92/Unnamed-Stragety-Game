using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemCancelHandler : EventTrigger {

    public override void OnCancel(BaseEventData data)
    {
        GameObject.Find("BattleMenuManager").GetComponent<BattleMenuManager>().reallowCursorMovement();
        GameObject.Find("Cursor").GetComponent<Cursor>().targetSelectionMode = 0;
    }

}
