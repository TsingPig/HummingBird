using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Machine Learning Agent��ǿ��ѧϰ������
/// </summary>
public class HammingBirdAgent : Agent
{

    [Tooltip("�Ƿ�����ѵ��ģʽ�£�trainingMode��")]
    public bool trainingMode;


    [Tooltip("๼�BeakTip�ı任")]
    public Transform beakTip;

    [Tooltip("�����������")]
    public Camera agentCamera;

    /// <summary>
    /// �Ѿ���õĻ���
    /// </summary>
    public float NectarObtained
    {
        get;
        private set;
    }

    /// <summary>
    /// �����������λ��
    /// </summary>
    public Vector3 BirdCenterPosition
    {
        get { return transform.position; }
        private set { transform.position = value; }
    }
    //�ȼ��ڣ�public Vector3 BirdCenterPosition=>transform.position;

    /// <summary>
    /// ���λ��
    /// </summary>
    public Vector3 BeakTipCenterPosition
    {
        get { return beakTip.position; }
        private set { beakTip.position = value; }
    }


    [Tooltip("���ƶ���ʱ��ʩ����С�����ϵ���")]
    public float moveForce = 2f;

    [Tooltip("���ϻ������µĸ�����ת�ٶ�")]
    public float pitchSpeed = 100f;
    public float maxPitchAngle = 80f;       //��󸩳�Ƕ�

    [Tooltip("Y��ƫ����ת���ǣ��ٶ�")]
    public float yawSpeed = 100f;

    //ƽ���ı丩���ƫ�����ٶ��ʣ�-1f~1f��
    private float smoothPitchSpeedRate = 0f;
    private float smoothYawSpeedRate = 0f;
    private float smoothChangeRate = 2f;





    //���������ж������������ͬ���ĳ�Ա�����������˻���ĳ�Ա��
    //ʹ��new�ؼ�����ʾ�����ػ����еĳ�Ա
    new private Rigidbody rigidbody;
    private FlowerArea flowerArea;      //agent���ڴ��ڵ�flowerArea����һ��һ����
    private Flower nearestFlower;       //��agent����Ļ�



    private const float BeakTipRadius = 0.008f; //๼��뻨�۵�������ײ����

    private bool frozen = false;          //Agent�Ƿ��ڷǷ���״̬



    /// <summary>
    /// ��ʼ��������
    /// </summary>
    public override void Initialize()
    {
        //override��дvirtual��������base.Initialize()��ʾ���ñ���д��������෽��
        //��������൱�ڶ��鷽�����й��ܵ����䡣
        base.Initialize();
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();      //С����FlowerArea��ֱ�Ӻ���
        //MaxStep����������ѵ��ģʽ�£���ĳ���������ܹ�ִ�е������
        if (!trainingMode)
        {
            MaxStep = 0;         //��ѵ��ģʽ�£�����������ƣ�MaxStep=0��
        }
    }

    /// <summary>
    /// ��һ��ѵ���غ�(Episode)��ʼ��ʱ���������������
    /// ���ǻὫ����������ٶ�״̬������״̬��������ѵ��ģʽ�£�����С������
    /// �Ļ�������<see cref="FlowerArea"/>����������Ƿ�Ҫ�ڻ�ǰ���ѡ�
    /// Ȼ���ƶ����µ����λ�á������¼��㵱ǰ����Ļ���
    /// </summary>
    public override void OnEpisodeBegin()
    {
        NectarObtained = 0;                     //���û�õĻ���
        rigidbody.velocity = Vector3.zero;       //���ٶȺͽ��ٶȹ���
        rigidbody.angularVelocity = Vector3.zero;

        //Ĭ������£�Ҫ����
        bool inFrontOfFlower = true;

        //base.OnEpisodeBegin();
        if (trainingMode)
        {
            //��ѵ��ģʽ�¡���ÿ��������flowerArea��ֻ��һ��������Agent��ʱ��
            //��ʱ��һֻ������һ�����������ϡ�
            flowerArea.ResetFlowerPlants();

            //��50%���������С���泯��
            inFrontOfFlower = Random.value > .5f;
        }

        //������������ƶ���һ���µĵ�λ
        MoveToSafeRandomPosition(inFrontOfFlower);
        //���������ƶ������¼�������Ļ�
        UpdateNearestFlower();

    }

    /// <summary>
    /// ��ÿ��Agent����������ҡ��������������ʽ�ľ���ʵ�壩���յ�һ������Ϊ��ʱ����á�(action received)
    /// ���ݽ��յ�����Ϊ����Agent��״̬��ִ���ض��Ķ����򴥷���ص��¼���
    /// �������������ݲ�ͬ����Ϊ������Agent����Ϸ�н�����Ӧ�Ĳ����;��ߣ�
    /// ��ʵ�����������Ϊ���ƺ�ѧϰ��
    /// Index 0:x����ı�����+1=���ң�-1=����
    /// Index 1:y����ı���(+1=up,-1=down)
    /// Index 2:z����ı���(+1=forward,-1=backward)
    /// Index 3:����Ƕȸı���(+1=pitch up,-1=pitch down)
    /// Index 4:ƫ���Ƕȸı���(+1=yaw turn right,-1=yaw turn left)
    /// </summary>
    /// <param name="actions">ActionBuffers���Ͷ���
    /// ���ڴ洢���յ�����Ϊ����Ϣ��ͨ������actions��������Ժͷ�����
    /// ���Ի�ȡ�ͽ�����Ϊ�ľ������ݣ�
    /// ����λ�ơ���ת�������ȡ�</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (frozen) return;
        //��ȡ������Ϊ������
        var vectorAction = actions.ContinuousActions;
        //����Ŀ���ƶ�����, targetDirection(dx,dy,dz)
        Vector3 targetMoveDirection = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        //�����С���ƶ������ϣ�ʩ��һ����
        rigidbody.AddForce(targetMoveDirection * moveForce);


        //��õ�ǰ��ת��״̬(������ת�ĽǶȶ���ŷ���ǣ�������������ת��ŷ����
        Vector3 curRotation = transform.rotation.eulerAngles;

        //��������Ϊ�м��㸩����ٶ��ʣ�-1~1����ƫ�����ٶ��ʣ�-1~1��
        float targetPitchSpeedRate = vectorAction[3];
        float targetYawSpeedRate = vectorAction[4];

        //ƽ�����㣬��smoothƽ��������ɵ�targetDelta�ϡ�
        //smooth���м���̴���ǰ�Ѿ����㵽�ġ�Ӧ�ø��ӵı仯����
        smoothPitchSpeedRate = Mathf.MoveTowards(smoothPitchSpeedRate, targetPitchSpeedRate, smoothChangeRate * Time.fixedDeltaTime);
        smoothYawSpeedRate = Mathf.MoveTowards(smoothYawSpeedRate, targetYawSpeedRate, smoothChangeRate * Time.fixedDeltaTime);
        //p+=Rdp*dp*dt,y=Rdy*dy*dt
        float pitch = curRotation.x + smoothPitchSpeedRate * Time.fixedDeltaTime * pitchSpeed;
        float yaw = curRotation.y + smoothYawSpeedRate * Time.fixedDeltaTime * yawSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

        //������󣬽��µõ�����ת�Ƕȸ��ǵ���ǰ��ת״̬��
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    /// <summary>
    /// �����������ռ��۲����ݵ���Ϊ����λ�ȡ�ʹ���۲����ݣ�
    /// �Ա����ѵ���;��ߡ�
    /// </summary>
    /// <param name="sensor">�������������������������������嵱ǰ״̬����������Ϣ��
    /// ���������۲�����///</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //sensor.AddObservation(�۲�����)���ڽ��۲�������ӵ��������֪��������ѵ��������
        //������Ļ���û�����ó�����ʱ��Ҫ���ݽ�ȥһ���յ�10άfloat����
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }
        //��ӣ�����ڸ�����ľֲ���ת���������С������ת��4��
        //��λ��Ԫ���ǳ���Ϊ1����Ԫ�������ڱ�ʾ��ת����
        Quaternion relativeRotation = transform.localRotation.normalized;
        //��ӣ�ָ�򻨵�����(3)
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - BeakTipCenterPosition;
        //toFlower.Normalize();
        //��ӣ��ж������Ƿ��򻨿���(+1����ֱ���ڻ���ǰ��-1�����ڻ����棩(1)
        //��������ˣ�A dot B > 0��ʾ������ͬ����ʾ�泯���� <0�෴��Ϊ0��ֱ
        float positionAlignment = Vector3.Dot(toFlower.normalized,
            -nearestFlower.FlowerUpVector.normalized);
        //��ӣ��ж��Ƿ���๳��򻨿���(�������ʾ��๳��򻨿��ڣ�(1)
        float beakTipAlignment = Vector3.Dot(beakTip.forward.normalized,
            -nearestFlower.FlowerUpVector.normalized);
        //��ӣ���๵�������ԣ����С�������루1��
        float relativeDistance = toFlower.magnitude / FlowerArea.areaDiameter;
        sensor.AddObservation(relativeRotation);
        sensor.AddObservation(toFlower.normalized);
        sensor.AddObservation(positionAlignment);
        sensor.AddObservation(beakTipAlignment);
        sensor.AddObservation(relativeDistance);
        //�ܹ�10���۲�

    }

    /// <summary>
    /// �����������Ϊ�������ͱ�����Ϊ"Heuristic Only"�������������
    /// ����ֵ�������ݵ�<see cref="OnActionReceived(ActionBuffers)"/>
    /// ������ʽģʽ�£��ֶ���д���������Ϊ�߼�������ʽ�㷨����������
    /// Index 0:x����ı�����+1=���ң�-1=����
    /// Index 1:y����ı���(+1=up,-1=down)
    /// Index 2:z����ı���(+1=forward,-1=backward)
    /// Index 3:����Ƕȸı���(+1=pitch up,-1=pitch down)
    /// Index 4:ƫ���Ƕȸı���(+1=yaw turn right,-1=yaw turn left)
    /// </summary>
    /// <param name="actionsOut">�洢���������Ϊ���</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Vector3 left = Vector3.zero;     //x
        Vector3 up = Vector3.zero;       //y
        Vector3 forward = Vector3.zero; //z
        float pitch = 0f;
        float yaw = 0f;
        //�û��������
        //���û������ʾ���ƶ�����ת��ӳ�䵽����������͸�����
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = (-1f) * transform.forward;

        if (Input.GetKey(KeyCode.LeftArrow)) left = (-1f) * transform.right;
        else if (Input.GetKey(KeyCode.RightArrow)) left = transform.right;

        if (Input.GetKey(KeyCode.Space)) left = transform.up;
        else if (Input.GetKey(KeyCode.LeftControl)) left = (-1f) * transform.up;

        if (Input.GetKey(KeyCode.UpArrow)) pitch = -1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = 1f;

        if (Input.GetKey(KeyCode.A)) yaw = -1f;
        else if (Input.GetKey(KeyCode.D)) yaw = 1f;

        Vector3 combinedDirection = (forward + up + left).normalized;

        actionsOut.ContinuousActions.Array[0] = combinedDirection.x;
        actionsOut.ContinuousActions.Array[1] = combinedDirection.y;
        actionsOut.ContinuousActions.Array[2] = combinedDirection.z;
        actionsOut.ContinuousActions.Array[3] = pitch;
        actionsOut.ContinuousActions.Array[4] = yaw;
    }

    /// <summary>
    /// ����ҿ���ģʽ�£����ƶ���������
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "ѵ��ģʽ��֧�ֶ��������塣");
        frozen = true;
        rigidbody.Sleep();
    }

    /// <summary>
    /// ����ҿ���ģʽ�£��ⶳ������
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "ѵ��ģʽ��֧�ֽⶳ�����塣");
        frozen = false;
        rigidbody.WakeUp();
    }

    /// <summary>
    /// �������������ƶ���һ����ȫ�����粻����ײ���ڣ���λ�ô���
    /// ������ڻ�ǰ�棬ͬʱ��Ҫ������뵽����
    /// </summary>
    /// <param name="inFrontOfFlower">�Ƿ�Ҫѡ��һ����ǰ��ĵ㡣</param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;        //���Ա�������������ܹ����Դ�����
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        //һֱѭ��ֱ���ҵ�һ�����Եĵ㡢���߳������Ա���������
        while (!safePositionFound && attemptsRemaining > 0)
        {
            if (inFrontOfFlower)
            {   //��Ҫ�泯��
                //�����ѡһ�仨
                int flowersCount = flowerArea.Flowers.Count;
                Flower randomFlower = flowerArea.Flowers[Random.Range(0, flowersCount)];

                //�������� 10~20cm�ľ��� ��disance*�����ϵĿ�������=ƫ����Offset��
                float distanceFromFlower = Random.Range(.1f, .2f);
                //ƫ����
                Vector3 offset = randomFlower.FlowerUpVector * distanceFromFlower;
                //Ǳ��λ��
                potentialPosition = randomFlower.transform.position + offset;

                //��Ǳ��λ��ָ�����ĵ�����
                Vector3 toVFlower = potentialPosition - randomFlower.FlowerCenterPosition;
                //������ת��ͷ������

                /*
                 Z axis will be aligned with forward, X axis aligned with cross product between forward 
                and upwards, and Y axis aligned with cross product between Z and X.
                 */
                potentialRotation = Quaternion.LookRotation(toVFlower, Vector3.up);
                //Ҳ����˵��LookRotation������������������ʱ�����Ƚ�Z�����Forward������
                //�����õڶ�������Լ�����µİ�緶Χ��
                //��������򻨵����ģ����Ҳ���ߵ���
            }
            else
            {   //����Ҫ�泯����ֻ��Ҫ���һ�������ϵ�λ�á��������һ����ظ߶�
                float height = Random.Range(1.2f, 2.5f);
                //�������һ�������������ĵľ���
                float radius = Random.Range(2f, 7f);
                //���ѡ��һ����������Y����ת
                Quaternion direction = Quaternion.AngleAxis(Random.Range(-180f, 180f), Vector3.up);

                /*
                 ��Ԫ����Quaternion����������Vector3��֮��ĳ˷����㲢���Ǵ�ͳ�������˷���
                ��Unity�У���һ����Ԫ����Quaternion����һ��������Vector3�����ʱ��ʹ�õ�����Ԫ������ת������
                �����˵��ͨ������Ԫ����ʾ����תӦ��������������ʵ�ֽ�������������ת����ʾ���������ת��Ч����
                 */
                //��Ԫ����������˱�ʾ����������������Ԫ��������ת֮��õ����µ�������
                Vector3 offset = Vector3.up * height + direction * Vector3.forward * radius;
                potentialPosition = flowerArea.FlowerAreaCenter + offset;

                //�����������(Pitch)��ƫ��(Yaw)
                float pitch = Random.Range(-60f, 60f);          //���尴��x����ת
                float yaw = Random.Range(-180f, 180f);           //ƫ������y����ת
                potentialRotation = Quaternion.Euler(pitch, yaw, 0);

            }
            //��Ҫ�ж��Ƿ���λ���Ƿ�������ײ
            safePositionFound = Physics.CheckSphere(potentialPosition, 0.05f);
            attemptsRemaining--;
        }
        //��ѭ��������ʱ��Ҫô��������������safe..=false)��Ҫô�ҵ���ȫλ��
        //Debug.Assert(condition,message)����һ�����������ж���
        //����������Ϊ�ٵ�ʱ�򣬴�ӡ��message
        Debug.Assert(safePositionFound, "û���ҵ���ȫ���ʵ����λ��");

        BirdCenterPosition = potentialPosition;
        //transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }


    /// <summary>
    /// ���������ƶ�������Ļ�����
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (var flower in flowerArea.Flowers)
        {
            //��ʼ״̬û���ҵ�����Ļ������л��۵Ļ�����Ϊ����Ļ���
            if (nearestFlower == null && flower.HasNectar)
            {
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                //��������Ļ�
                float distanceToFlower = Vector3.Distance(flower.FlowerCenterPosition, BeakTipCenterPosition);
                float distanceToNearestFlower = Vector3.Distance(nearestFlower.FlowerCenterPosition, BeakTipCenterPosition);
                //�����仨�Ѿ���л�ˣ�û�л��ۣ��������ҵ��������л��۵Ļ�
                if (nearestFlower.HasNectar == false || distanceToFlower < distanceToNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }

    /// <summary>
    /// ����������ͣ���¼�:�����������ײ�崥�������۵�ʱ���п�������������ⲿ�ֽӴ���
    /// ������Ҫȷ������๽��뻨�۵�ʱ�򣬲Ž��н������ơ�
    /// </summary>
    /// <param name="collider">������ײ��</param>
    private void OnTriggerStay(Collider collider)
    {
        if (collider.CompareTag("Nectar"))
        {
            Vector3 closePointToBeakTip = collider.ClosestPoint(BeakTipCenterPosition);

            //��ʾ����ܹ��Ե�����
            if (Vector3.Distance(closePointToBeakTip, BeakTipCenterPosition) < BeakTipRadius)
            {
                //�ҵ�������ײ���Ӧ��Flower��
                Flower flower = flowerArea.GetFlowerFromNectar(collider);
                //����ȥ�Ե�0.01f�Ļ��ۡ�
                //��//ע�⣺����¼���ÿ0.02�뷢��һ�Ρ�һ�뷢��50�Ρ�
                float nectarReceived = flower.Feed(.01f);
                NectarObtained += nectarReceived;
                if (trainingMode)
                {
                    ////��������ˣ�A dot B > 0��ʾ������ͬ����ʾ�泯���� <0�෴��Ϊ0��ֱ
                    ////��ӣ��ж��Ƿ���๳��򻨿���(�������ʾ��๳��򻨿��ڣ�(1)
                    //float beakTipAlignment = Vector3.Dot(beakTip.forward.normalized,
                    //-nearestFlower.FlowerUpVector.normalized);
                    float forwardAlignment = Vector3.Dot(transform.forward.normalized,
                        -nearestFlower.FlowerUpVector.normalized);
                    //��������0.01f������������Ż����в�ʳ����������0.02f�֡�
                    float bonus = .02f * Mathf.Clamp01(forwardAlignment);
                    float baseIncrement = .01f;
                    float increment = baseIncrement + bonus;
                    AddReward(increment);
                }
            }
            //�ǵø���flower
            if (!nearestFlower.HasNectar)
            {
                UpdateNearestFlower();
            }
        }
    }

    /// <summary>
    /// ײ�Ϲ�����ײ��
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Boundary") && trainingMode)
        {
            //��ײ�Ϲ���߽磺������ľ���أ��������帺�ķ���
            AddReward(-0.5f);

        }
    }

    private void Update()
    {
        //��һ�������ָ������Ļ�����
        if (nearestFlower != null)
        {
            Debug.DrawLine(BeakTipCenterPosition, nearestFlower.FlowerCenterPosition, Color.green);


        }
    }
    private void FixedUpdate()
    {
        //Ҫ���Ƕ������߻���ʱ�򣬸���
        if (nearestFlower != null && nearestFlower.HasNectar == false)
        {
            UpdateNearestFlower();
        }
    }
}
