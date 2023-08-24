using UnityEngine;

/// <summary>
/// ����һ�仨
/// </summary>
public class Flower : MonoBehaviour
{
    //Tooltip����ʹ����Inspector���ܹ���ʾ����ʾЧ��
    [Tooltip("������(fullFlower)��ʱ�����ɫ")]
    public Color fullFlowerColor = new Color(1f, 0, .3f);

    [Tooltip("������(emtpyFlower)��ʱ�����ɫ")]
    public Color emtpyFlowerColor = new Color(.5f, 0, 1f);

    [HideInInspector]
    public Collider nectarCollider;     //���۴�����
    private Collider flowerCollider;    //����ײ��
    private Material flowerMaterial;    //������

    private float nectarAmount;         //��������

    //ʡȥset��������ʾֻ���������Ը�ֵ
    //private set˽�������������������ڲ����и�ֵ
    public Vector3 FlowerUpVector       //ÿһ�仨�Ŀ��ڷ���
    {
        get { return nectarCollider.transform.up; }
    }
    public Vector3 FlowerCenterPosition //��������λ��
    {
        get { return nectarCollider.transform.position; }
    }
    //private void Start()
    //{
    //    FlowerCenterPosition = 1;
    //    NectarAmount= 1;
    //}
    public float NectarAmount           //���۵�����
    {
        get { return nectarAmount; }
        private set { nectarAmount = value > 0 ? value : 0; }
    }

    public bool HasNectar               //�Ƿ��л���
    {
        get { return NectarAmount > 0; }
    }

    /// <summary>
    /// ����ȥ�Ե�����
    /// </summary>
    /// <param name="amount">���ԳԵ�������</param>
    /// <returns>ʵ�ʳԵ�������</returns>
    public float Feed(float amount)
    {
        //Clamp(A,mi,mx)��A������[mi,mx]֮�䲢����ʵ�����Ƶ�ֵ
        //takenAmount��amount���ڶ���ʾʵ�ʳԵ���ֵ
        float takenAmount = Mathf.Clamp(amount, 0f, NectarAmount);

        NectarAmount -= amount;

        if (HasNectar == false)
        {
            //����û���Ժ���Ҫ������ײ�塢���۴����� ɾ��
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);
            //��ʹ�� Unity ��������ɫ��ʱ��ͨ����ʹ�� _BaseColor ����ʾ���ʵ�
            //��Ҫ��ɫ����������ɫ��
            flowerMaterial.SetColor("_BaseColor", emtpyFlowerColor);

        }
        return takenAmount;
    }

    /// <summary>
    /// �ָ�����״̬
    /// </summary>
    public void ResetFlower()
    {
        NectarAmount = 1f;
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }

    private void Awake()
    {
        //�ҵ�����������Ⱦ��(Mesh Renderer)
        //Material material=GetComponent<Material>();������д��������
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        flowerCollider = transform.GetChild(0).GetComponent<Collider>();
        nectarCollider = transform.GetChild(1).GetComponent<Collider>();

    }

}
