using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class Cat : MonoBehaviour {

    public int Mood; //心情
    public System.DateTime LastFeedTime; //上次喂食时间
    public int FeedTime; //喂的次数

    enum CatStates
    {
        Happy, //开心（到处玩）
        Sleep, //睡觉
        Angry,//生气
        Hungry, //饿了
        Boring, //无聊
    }

    public string Name;

    Animator animator;
    CatStates m_CurrentStates = CatStates.Happy;
    

    // Use this for initialization
    void Start () {
        animator = GetComponent<Animator>();

        Idle();
        NextStates();
    }

    void NextStates()
    {
        StartCoroutine(DONextStates());
    }

    IEnumerator DONextStates()
    {
        yield return new WaitForSeconds(Random.Range(1f, 4f));
        StatesTrigger();
    }

    IEnumerator CallWithDelay(System.Action callback, float time)
    {
        yield return new WaitForSeconds(time);
        callback();
    }
    
    void SwitchStatesTo(CatStates states)
    {
        m_CurrentStates = states;
        StatesTrigger();
    }

    public void Sound()
    {
        AudioSource.PlayClipAtPoint(SillkyCat.Instance.catSound, transform.position);
    }

    void StatesTrigger()
    {
        //不管什么状态，5%的概率拉一坨屎
        if(Random.Range(0,100) <= 5)
        {
            Shit();
            Sound();
        }

        //不管什么状态，10%概率增加钱
        if(Random.Range(0,100) <= 10)
        {
            SillkyCat.Instance.AddMoney();
            Sound();
        }

        switch (m_CurrentStates)
        {
            case CatStates.Happy:

                int selectCase = Random.Range(0, 100);

                //25%概率换个地方
                if(selectCase >=0 && selectCase < 25)
                {
                    var dest = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                    var speed = Random.Range(0.1f, 1f);
                    var time = dest.magnitude / speed;

                    animator.SetTrigger("DoMove");
                    animator.SetFloat("Speed", speed);
                    transform.DOLookAt(dest, 0.5f);
                    transform.DOMove(dest, time).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        Idle();
                        NextStates();
                    });
                }else if(selectCase >= 25 && selectCase < 50) 
                {
                    animator.SetTrigger("DoWash");
                    NextStates();
                }
                else if (selectCase >= 50 && selectCase < 70) 
                {
                    animator.SetTrigger("DoCry");
                    NextStates();
                }
                else if(selectCase >= 70 && selectCase < 80) 
                {
                    animator.SetTrigger("DoDrink");
                    NextStates();
                }
                else
                {
                    SwitchStatesTo(CatStates.Sleep);
                }
                break;
            case CatStates.Sleep:
                {
                    animator.SetTrigger("DoSleep");
                    float sleepTime = Random.Range(10f,30f);
                    StartCoroutine(CallWithDelay(()=> {
                        SwitchStatesTo(CatStates.Happy);
                    }, sleepTime));
                    break;
                }
            default:
                break;
        }
    }

    void Idle(float value = -1)
    {
        animator.SetTrigger("DoIdle");
        if(value == -1)
        {
            value = Random.Range(0f, 5f);
        }
        animator.SetFloat("IdleValue", value);
    }

    // Update is called once per frame
    void Update () {

    }

    //喂食
    public void Feed()
    {
        LastFeedTime = System.DateTime.Now;
    }

    //拉屎
    public void Shit()
    {
        SillkyCat.Instance.MakeShit(this);
    }
    
    //逗猫
    public void PlayWith()
    {
        if (m_CurrentStates == CatStates.Sleep || m_CurrentStates == CatStates.Angry)
            return;

        StopAllCoroutines();
        CancelInvoke();
        this.DOKill();

        var pos = SillkyCat.Instance.mainCamera.transform.position;

        var dest = new Vector3(pos.x + Random.Range(-0.2f, 0.2f), 0, pos.z + Random.Range(-0.2f, 0.2f));
        var speed = Random.Range(0.8f, 1f);
        var time = dest.magnitude / speed;

        animator.SetTrigger("DoMove");
        animator.SetFloat("Speed", speed);
        transform.DOLookAt(dest, 0.5f);
        transform.DOMove(dest, time).SetEase(Ease.Linear).OnComplete(() =>
        {
            animator.SetTrigger("DoPlay");
        });
    }

    public void PlayWithOff()
    {
        StatesTrigger();
    }
}
