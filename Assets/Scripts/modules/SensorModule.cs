using System;
using UnityEngine;
using System.Collections;

// Pitch Range: -90 - 90
// Roll Range: -180 - 180
// Yaw Range: -180 - 180

public class SensorModule : Module
{
    // sub physics
    private Rigidbody rb;
	private float sigma = 1.0f;
	private float mu = 0.0f;

    // sensor packet
    private float pitch;
    private float roll;
    private float yaw;
    private float depth;
    private float battery;
    private int start_switch;
    private float dpitch;
    private float droll;
    private float dyaw;
    private float ddepth;

    public SensorModule(Rigidbody rb)
    {
        this.rb = rb;
    }

    protected override void init()
    {
        // init sensors
        pitch = 0;
        roll = 0;
        yaw = 0;
        depth = 0;
        battery = 20;
        start_switch = 0;
        dpitch = 0;
        droll = 0;
        dyaw = 0;
        ddepth = 0;
    }

    protected override void update()
	{
        // update sensors
        UpdateDepth();
        UpdateYPR();

        // send sensor messages
        SendSensorMessage("control");
        //SendSensorMessage("control_gui");
        //SendSensorMessage("helm");
        //SendSensorMessage("ai");
    }

    protected override void shutdown()
    {

    }

    void UpdateDepth()
    {
        GameObject water = GameObject.Find("WaterTop");
        float watery = water.GetComponent<Transform>().position.y;
        float suby = rb.position.y;
        float last_depth = depth;
        depth = suby - watery;
        ddepth = (depth - last_depth) / dt;
    }

    public void UpdateYPR()
    {
        Vector3 sub_forward = rb.transform.forward;
        Vector3 sub_right = rb.transform.right;
        Vector3 vec = new Vector3();

        // Yaw is angle around global y (vertical) axis
        yaw = Mathf.Atan2(sub_forward.x, sub_forward.z);

        // Yaw subs forward vector back to 0 yaw so that it can be put into atan2 for pitch
        vec.x =  sub_forward.x * Mathf.Cos(-yaw) + sub_forward.z * Mathf.Sin(-yaw);
        vec.y =  sub_forward.y;
        vec.z = -sub_forward.x * Mathf.Sin(-yaw) + sub_forward.z * Mathf.Cos(-yaw);

        // Pitch is angle around subs x (right) axis
        pitch = Mathf.Atan2(vec.y, vec.z);

        // Yaw subs right vector so that it can be put into atan2 for roll
        vec.x =  sub_right.x * Mathf.Cos(-yaw) + sub_right.z * Mathf.Sin(-yaw);
        vec.y =  sub_right.y;
        vec.z = -sub_right.x * Mathf.Sin(-yaw) + sub_right.z * Mathf.Cos(-yaw);

        // Roll is angle around subs z (forward) axis
        roll = Mathf.Atan2(vec.y, vec.x);

        // convert to degrees
        yaw = -ToDegrees(yaw);
        pitch = -ToDegrees(pitch);
        roll = -ToDegrees(roll);

        // Convert from 0 - 360 -> -180 - 180
        if(yaw > 180.0f)
            yaw = 180.0f - yaw;

        //Debug.Log("Pitch: " + pitch);
        //Debug.Log("Roll: " + roll);
        //Debug.Log("Yaw: " + yaw);

        // add some noise
		yaw += RandomGaussian(sigma,mu);
		pitch += RandomGaussian(sigma,mu);
		roll += RandomGaussian(sigma,mu);
    }

    public void SendSensorMessage(string name)
    {
        //sensor_packet sp = new sensor_packet((double)pitch, (double)roll, (double)yaw, (double)depth, (double)battery, start_switch, (double)dt,
                                             //(double)dpitch, (double)droll, (double)dyaw, (double)ddepth);
        Quaternion q = rb.transform.rotation;

        sensor_packet sp = new sensor_packet((double)q.w, (double)q.x, (double)q.y, (double)q.z, (double)depth, (double)battery, start_switch, (double)dt, (double)ddepth);
        message msg = new message("sensor", name, "sensor", sp.whole);
        com.send_message(msg);
    }

    public float ToRadians(float theta)
	{
		return Mathf.Deg2Rad * theta;
	}

    public float ToDegrees(float theta)
	{
		return Mathf.Rad2Deg * theta;
	}

	public float RandomGaussian(float sigma, float mu)
	{
		System.Random rand = new System.Random();
		double u1 = rand.NextDouble();
		double u2 = rand.NextDouble();
		double x = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
		return (float) x * sigma + mu;
	}
}
