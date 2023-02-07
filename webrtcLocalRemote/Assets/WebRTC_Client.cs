using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using UnityEngine.Timeline;

public class WebRTC_Client : MonoBehaviour
{

    private RTCPeerConnection localPeer;
    private RTCPeerConnection remotePeer;

    [SerializeField] private AudioSource _inputAudio;
    [SerializeField] private AudioSource _outputAudio;

    private MediaStream localStream;
    private MediaStream remoteStream;

    private string microphone = null;
    //private AudioClip m_clipInput;
    private AudioStreamTrack m_audioTrack;

    int m_samplingFrequency = 48000;
    int m_lengthSeconds = 1;

    void Start()
    {


        microphone = Microphone.devices[0];
        var m_clipInput = Microphone.Start(microphone, true, m_lengthSeconds, m_samplingFrequency);
        Debug.Log("Selected microphone: " + microphone);

        _inputAudio.loop = true;
		_inputAudio.clip = m_clipInput;
        _inputAudio.Play();

		m_audioTrack = new AudioStreamTrack(_inputAudio);

        Debug.Log("starting");
        CreatePeers();
    }

    void OnAddTrack(MediaStreamTrackEvent e)
    {
        Debug.Log("OnAddTrack");
        var track = e.Track as AudioStreamTrack;
        _outputAudio.SetTrack(track);
        _outputAudio.loop = true;
        _outputAudio.Play();
    }

    private void CreatePeers()
	{
        Debug.Log("creating peers");
        localPeer = new RTCPeerConnection();
        

        localStream = new MediaStream();
        remoteStream = new MediaStream();

        m_audioTrack.Loopback = true;
        remoteStream.OnAddTrack += OnAddTrack;

        localPeer.OnIceCandidate = e =>
        {
            remotePeer.AddIceCandidate(e);
        };
        localPeer.OnNegotiationNeeded = () => StartCoroutine(ExchangeOffer());
        localPeer.OnIceConnectionChange = (e) =>
        {
            Debug.Log($"Local: IceConnectionChange: {e}");
        };
        localPeer.OnConnectionStateChange = (e) =>
        {
            Debug.Log($"Local: ConnectionStateChange: {e}");
        };
        localPeer.OnIceGatheringStateChange = (e) =>
        {
            Debug.Log($"Local: IceGatheringStateChange: {e}");
        };

        remotePeer = new RTCPeerConnection();

        remotePeer.OnIceCandidate = e =>
        {
            localPeer.AddIceCandidate(e);
        };
        remotePeer.OnIceConnectionChange = (e) =>
        {
            Debug.Log($"Remote: IceConnectionChange: {e}");
        };
        remotePeer.OnConnectionStateChange = (e) =>
        {
            Debug.Log($"Remote: ConnectionStateChange: {e}");
        };
        remotePeer.OnIceGatheringStateChange = (e) =>
        {
            Debug.Log($"Remote: IceGatheringStateChange: {e}");
        };

        var transceiver2 = remotePeer.AddTransceiver(TrackKind.Audio);
        transceiver2.Direction = RTCRtpTransceiverDirection.RecvOnly;

        remotePeer.OnTrack = (RTCTrackEvent e) => handleRemoteTrack(e);

        
        localPeer.AddTrack(m_audioTrack, localStream);
        //yield return StartCoroutine(ExchangeOffer());
	}

    public void handleRemoteTrack(RTCTrackEvent e)
    {
        Debug.Log("HandleRemoteTrack");
        if (e.Track is AudioStreamTrack)
        {
            remoteStream.AddTrack(e.Track);
        }
    }

    private IEnumerator ExchangeOffer()
	{
        Debug.Log("Creating offer");
        var op1 = localPeer.CreateOffer();
        yield return op1;
        Debug.Log("Offer created");
        var desc = op1.Desc;
        var op2 = localPeer.SetLocalDescription(ref desc);
        yield return op2;
        Debug.Log("Local negotiation done. Waiting for answer.");
        desc = op1.Desc;
        Debug.Log("Setting remote description");
        var op3 = remotePeer.SetRemoteDescription(ref desc);
        yield return op3;
        Debug.Log("Creating answer");
        var op4 = remotePeer.CreateAnswer();
        yield return op4;
        Debug.Log("Answer created");
        desc = op4.Desc;
        var op5 = remotePeer.SetLocalDescription(ref desc);
        yield return op5;
        Debug.Log("Remote negotiation done. Add answer to local peer.");
        desc = op4.Desc;
        var op6 = localPeer.SetRemoteDescription(ref desc);
        yield return op6;
        Debug.Log("Done");
    }

}
