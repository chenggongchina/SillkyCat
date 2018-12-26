using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SillkyCat : MonoBehaviour {

    const int MAX_CAT_ID = 5;
    public static SillkyCat Instance;

    public Camera mainCamera;
    public Text logText;
    public Text infoText;
    public GameObject shitPrefab;
    public Transform shitRoot;
    public GameObject toy;
    public AudioClip catSound;

    public GameObject messageBox;
    public Text messageBoxText;

    public List<GameObject> catsPrefabs;

    public Slider cameraSlider1;
    public Slider cameraSlider2;

    //runtime data
    public int Money
    {
        get
        {
            return _money;
        }
        set
        {
            _money = value;
            RefreshInfo();
        }
    }

    private int _money;
    [HideInInspector]
    public DateTime LastLogOffTime;//上次登出时间
    [HideInInspector]
    public List<Cat> Cats;

    // Use this for initialization
    void Start()
    {
        messageBox.gameObject.SetActive(false);
        Instance = this;
        //NewGame();
        ReadNetStream();
        Load();
    }

    void ReadNetStream()
    {
        try
        {
            WebRequest wr = WebRequest.Create("http://www.jy-x.com/jygame/sillkycat.txt");

            Stream s = wr.GetResponse().GetResponseStream();
            StreamReader sr = new StreamReader(s, Encoding.UTF8);
            string msg = sr.ReadToEnd();
            sr.Close();
            s.Close();

            if (!string.IsNullOrEmpty(msg))
            {
                messageBox.SetActive(true);
                messageBoxText.text = msg;
            }
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
            messageBox.SetActive(false);
        }
    }


    // Update is called once per frame
    void Update()
    {

    }


    public void OnQuit()
    {
        Save();
        Application.Quit();
    }

    void Save()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.Append(Money + "," + Cats.Count + "," + shitRoot.childCount);
        sb.AppendLine();
        foreach(var cat in Cats)
        {
            sb.Append(cat.Name);
            sb.AppendLine();
            sb.AppendFormat("{0},{1}", cat.transform.position.x, cat.transform.position.z); //坐标
            sb.AppendLine();
        }

        PlayerPrefs.SetString("save", sb.ToString());
        PlayerPrefs.Save();

        Debug.Log("save finished");
    }

    //挂机收益
    void TimePass(DateTime lastTime)
    {
        var timespan = DateTime.Now - lastTime;
        var totalHours = timespan.TotalHours;

        LogInfo("距离上次登录一共" + totalHours + "小时");
        int moneyGet = (int)(totalHours * 5 * Cats.Count);
        if (timespan.TotalDays >= 1)
            moneyGet += 200; //每天固定收益

        if(moneyGet > 0)
        {
            LogInfo("在离线这段时间，猫舍共收益$:" + moneyGet);
            Money += moneyGet;
        }

        //最多20个屎
        int shitGet = Math.Min((int)(totalHours * UnityEngine.Random.Range(0, 10) * Cats.Count), 20);
        
        if(shitGet > 0)
        {
            for(int i = 0; i < shitGet; ++i)
            {
                MakeRandomShit();
            }
        }
    }

    void Load()
    {
        LogInfo("loading..");
        try
        {
            if (PlayerPrefs.HasKey("save"))
            {

                string[] lines = PlayerPrefs.GetString("save").Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                var lastSaveTime = lines[0];

                var tmp = lines[1].Split(',');
                Money = int.Parse(tmp[0]);
                int catCount = int.Parse(tmp[1]);
                int shitCount = int.Parse(tmp[2]);
                int currentIndex = 2;
                for (int i = 0; i < catCount; ++i)
                {
                    int catId = int.Parse(lines[currentIndex]);
                    currentIndex++;
                    var pos = lines[currentIndex].Split(',');
                    float x = float.Parse(pos[0]);
                    float z = float.Parse(pos[1]);
                    currentIndex++;

                    GenerateCat(catId, x, z);
                }

                for (int i = 0; i < shitCount; ++i)
                {
                    MakeRandomShit();
                }

                var time = DateTime.ParseExact(lastSaveTime, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                TimePass(time);
            }

            else
            {
                NewGame();
            }
        }
        catch (Exception e)
        {
            LogInfo(e.ToString());
        }
    }

    void NewGame()
    {
        Money = 500;
        GenerateCat(1, 0.4f, 1.9f);
        GenerateCat(2, 0.28f, 2f);
    }




    public void SetCameraValue()
    {
        mainCamera.transform.rotation = Quaternion.Euler(cameraSlider2.value, cameraSlider1.value, 0);
    }


    void OnApplicationFocus(bool hasFocus)
    {
        Save();
    }

    public void AddMoney()
    {
        int addMoney = UnityEngine.Random.Range(1, 5);
        LogInfo("太萌了~~~吸猫的人付了$:" + addMoney);
        Money += addMoney;
    }

    public void RefreshInfo()
    {
        infoText.text = string.Format("$:{0}", Money);
    }

    //显示日志
    public void LogInfo(string msg)
    {
        logText.text = msg + "\n" + logText.text;

        if (clearLogInfoCoroutine != null)
        {
            StopCoroutine(clearLogInfoCoroutine);
        }
        clearLogInfoCoroutine = StartCoroutine(ClearLogInfo());
    }

    IEnumerator ClearLogInfo()
    {
        yield return new WaitForSeconds(3f);
        logText.text = "";
        clearLogInfoCoroutine = null;
    }
    Coroutine clearLogInfoCoroutine;

    //喂食
    public void Feed()
    {
        if (Money < 10)
        {
            LogInfo("钱不够，需要最少$10");
            return;
        }

        LogInfo("猫猫们吃饱了~");

        Money -= 10;
        DateTime min = DateTime.MaxValue;
        Cat feedCat = null;
        foreach(var cat in Cats)
        {
            if(cat.LastFeedTime < min)
            {
                min = cat.LastFeedTime;
                feedCat = cat;
            }
        }

        feedCat.Feed();
    }

    public void MakeShit(Cat cat)
    {
        if(shitRoot.childCount > 10)
        {
            LogInfo("<color=red>猫舍太脏了，没人愿意过来撸猫了。</color>");
            return;
        }

        var shit = Instantiate(shitPrefab) as GameObject;

        //增加一个随机值，防止全部拉在一起
        shit.transform.position = new Vector3(
            cat.transform.position.x + UnityEngine.Random.Range(-0.1f,0.1f), 
            0.0093f, 
            cat.transform.position.z + UnityEngine.Random.Range(-0.1f, 0.1f));

        shit.transform.SetParent(shitRoot);
    }

    void MakeRandomShit()
    {
        var shit = Instantiate(shitPrefab) as GameObject;

        //增加一个随机值，防止全部拉在一起
        shit.transform.position = new Vector3(
            UnityEngine.Random.Range(-3, 3),
            0.0093f,
            UnityEngine.Random.Range(-3, 3));

        shit.transform.SetParent(shitRoot);
    }

    public void ClearShit()
    {
        if (Money < 1)
        {
            LogInfo("钱不够，需要最少$1");
            return;
        }
        if(shitRoot.childCount == 0)
        {
            LogInfo("粑粑已经全部扫干净啦~");
            return;
        }
        Money -= 1;
        Destroy(shitRoot.GetChild(0).gameObject);
        LogInfo("扫掉了一个粑粑~");
    }

    public Cat GenerateCat(int id, float x, float z)
    {
        string path = "cat" + id;
        //var prefab = Resources.Load<GameObject>(path);
        var prefab = catsPrefabs[id - 1];

        var obj = Instantiate(prefab) as GameObject;
        var cat = obj.GetComponent<Cat>();
        cat.Name = id.ToString();
        cat.transform.position = new Vector3(x, 0, z);
        Cats.Add(cat);
        return cat;
    }

    public void BuyCat()
    {
        if (Money < 100)
        {
            LogInfo("钱不够，需要最少$100");
            return;
        }
        Money -= 100;

        //生成一只猫
        var cat = GenerateCat(UnityEngine.Random.Range(1, MAX_CAT_ID + 1), UnityEngine.Random.Range(-1f,1f), UnityEngine.Random.Range(-1f, 1f));
        cat.Sound();
    }

    public void PlayWithCat()
    {
        if (!toy.activeSelf)
        {
            toy.SetActive(true);
            mainCamera.transform.DORotateQuaternion(Quaternion.Euler(90, 180, 0), 1f);

            foreach (var cat in Cats)
            {
                cat.PlayWith();
            }
        }
        else
        {
            toy.SetActive(false);

            mainCamera.transform.DORotateQuaternion(Quaternion.Euler(20, 180, 0), 1f);

            foreach (var cat in Cats)
            {
                cat.PlayWithOff();
            }
        }
        
    }
}
