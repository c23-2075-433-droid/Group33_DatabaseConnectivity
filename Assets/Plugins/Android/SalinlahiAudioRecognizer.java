package ph.edu.uphsl.group33.salinlahi;

import android.app.Activity;
import android.content.Intent;
import android.media.AudioFormat;
import android.media.AudioRecord;
import android.media.MediaRecorder;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.ParcelFileDescriptor;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;
import android.widget.Toast;

import com.unity3d.player.UnityPlayer;

import java.io.IOException;
import java.io.OutputStream;
import java.util.ArrayList;
import java.util.concurrent.LinkedBlockingQueue;

/**
 * Listens for ONE answer the way SALINLAHI needs it for the six strictly
 * judged words (Tayo, Upo, Bukas, Basa, Bati, Pasok): the speech recognizer
 * says WHICH word was said, and the game keeps the SOUND so its pronunciation
 * model can say HOW it was said (tayô or táyo).
 *
 * Many phones (a Galaxy A03s among them) give the microphone to one user at a
 * time, so the game cannot record while the recognizer listens. Here the game
 * is the only one that records: AudioRecord reads the microphone, every block
 * is kept for the game AND written into a pipe, and the recognizer is told to
 * read its audio from that pipe (RecognizerIntent.EXTRA_AUDIO_SOURCE, Android
 * 13 / API 33 and later). Both hear exactly the same sound.
 *
 * Whether a phone's recognizer accepts audio this way is up to the phone, so
 * this reports every failure to Unity (VoiceCommand) and the game falls back
 * to its normal listening. Nothing is written to storage: the audio lives in
 * memory until the game has taken it.
 *
 * Unity side: AndroidAudioRecognizer.cs. Events arrive by UnitySendMessage on
 * the GameObject named in start(): OnBridgeReady, OnBridgeSpeechBegan,
 * OnBridgeResult(text), OnBridgeError(code).
 */
public class SalinlahiAudioRecognizer
{
    private static final int RATE = 16000;
    private static final int MAX_SECONDS = 12;

    // The RecognizerIntent extras added in API 33, spelled out so this compiles
    // whatever SDK the project builds against.
    private static final String EXTRA_AUDIO_SOURCE = "android.speech.extra.AUDIO_SOURCE";
    private static final String EXTRA_AUDIO_SOURCE_CHANNEL_COUNT = "android.speech.extra.AUDIO_SOURCE_CHANNEL_COUNT";
    private static final String EXTRA_AUDIO_SOURCE_ENCODING = "android.speech.extra.AUDIO_SOURCE_ENCODING";
    private static final String EXTRA_AUDIO_SOURCE_SAMPLING_RATE = "android.speech.extra.AUDIO_SOURCE_SAMPLING_RATE";

    private static SpeechRecognizer recognizer;
    private static AudioRecord record;
    private static Thread readThread, writeThread;
    private static volatile boolean running;
    private static ParcelFileDescriptor readEnd, writeEnd;
    private static LinkedBlockingQueue<short[]> toRecognizer;
    private static final Object lock = new Object();
    private static short[] audio = new short[RATE * MAX_SECONDS];
    private static int audioLength;
    private static float[] lastAnswer = new float[0];
    private static String target;

    // ---- End of speech --------------------------------------------------------------
    // A recognizer that reads its audio from a pipe answers when the pipe ENDS,
    // not when the speaker falls silent (tried on a Galaxy A03s: "speech began"
    // again and again, never a result). So the game decides when the child has
    // finished - the room's noise floor is followed, speech starts when the
    // sound stays 12 dB above it for 60 ms and ends after 700 ms within 7 dB
    // of it (or 4 s after it started) - and then closes the pipe.
    private static final short[] END_OF_STREAM = new short[0];
    private static final float START_DB = 12f, END_DB = 7f;
    // Shorter than before (35 / 200): tried on a Galaxy A03s, the game's own
    // music kept the room from ever sounding quiet, so the word only "ended"
    // at the 4-second limit. The recognizer's own end of speech (below) is now
    // the main signal; these are the backup.
    private static final int START_BLOCKS = 3, END_BLOCKS = 25, MIN_SPEECH_BLOCKS = 8, MAX_SPEECH_BLOCKS = 125;
    private static volatile boolean ended;

    // The last partial result. Tried on a Galaxy A03s: with the audio coming
    // from the pipe, the recognizer's partial results said "tayo" but its
    // FINAL result was empty, so an empty final falls back to this.
    private static volatile String lastPartial = "";
    private static volatile boolean speechEndedSeen;

    /** True when this phone could run it at all (Android 13+ and a recognizer exists). */
    public static boolean isSupported()
    {
        Activity a = UnityPlayer.currentActivity;
        return a != null && Build.VERSION.SDK_INT >= 33 && SpeechRecognizer.isRecognitionAvailable(a);
    }

    /**
     * Starts listening for one answer.
     * @param gameObject the Unity GameObject that receives the events
     * @param language e.g. "fil-PH"
     * @param onDevice true to ask for the on-device recognizer (API 31+)
     */
    public static void start(final String gameObject, final String language, final boolean onDevice)
    {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) return;
        target = gameObject;
        activity.runOnUiThread(new Runnable()
        {
            @Override public void run()
            {
                stopAll();
                try
                {
                    startRecording();
                    recognizer = onDevice && Build.VERSION.SDK_INT >= 31
                            ? SpeechRecognizer.createOnDeviceSpeechRecognizer(activity)
                            : SpeechRecognizer.createSpeechRecognizer(activity);
                    recognizer.setRecognitionListener(new Listener());

                    Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                    intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
                    intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 3);
                    intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);
                    if (language != null && language.length() > 0)
                        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, language.replace('_', '-'));
                    if (onDevice) intent.putExtra(RecognizerIntent.EXTRA_PREFER_OFFLINE, true);
                    intent.putExtra(EXTRA_AUDIO_SOURCE, readEnd);
                    intent.putExtra(EXTRA_AUDIO_SOURCE_CHANNEL_COUNT, 1);
                    intent.putExtra(EXTRA_AUDIO_SOURCE_ENCODING, AudioFormat.ENCODING_PCM_16BIT);
                    intent.putExtra(EXTRA_AUDIO_SOURCE_SAMPLING_RATE, RATE);
                    recognizer.startListening(intent);
                }
                catch (Throwable e)
                {
                    stopAll();
                    send("OnBridgeError", "-1 " + e.getClass().getSimpleName() + ": " + e.getMessage());
                }
            }
        });
    }

    /** Stops listening without a result. */
    public static void cancel()
    {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null) { stopAll(); return; }
        activity.runOnUiThread(new Runnable() { @Override public void run() { if (running) keepAnswer(); stopAll(); } });
    }

    /** The audio of the last answer (16 kHz mono, -1..1), from the start of listening to its result. */
    public static float[] takeAudio()
    {
        synchronized (lock) { float[] a = lastAnswer; lastAnswer = new float[0]; return a; }
    }

    public static int sampleRate() { return RATE; }

    /** Keeps what has been recorded so far as the answer, without stopping (for a round that timed out). */
    public static void keepNow() { keepAnswer(); }

    /** A short message on screen, for testing on a phone without a cable. */
    public static void toast(final String text)
    {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) return;
        activity.runOnUiThread(new Runnable()
        {
            @Override public void run() { Toast.makeText(activity, text, Toast.LENGTH_LONG).show(); }
        });
    }

    // ---- Recording -----------------------------------------------------------------

    private static void startRecording() throws IOException
    {
        int min = AudioRecord.getMinBufferSize(RATE, AudioFormat.CHANNEL_IN_MONO, AudioFormat.ENCODING_PCM_16BIT);
        record = new AudioRecord(MediaRecorder.AudioSource.VOICE_RECOGNITION, RATE,
                AudioFormat.CHANNEL_IN_MONO, AudioFormat.ENCODING_PCM_16BIT, Math.max(min * 2, RATE));
        if (record.getState() != AudioRecord.STATE_INITIALIZED)
            throw new IOException("the microphone could not be opened");

        ParcelFileDescriptor[] pipe = ParcelFileDescriptor.createPipe();
        readEnd = pipe[0];
        writeEnd = pipe[1];
        toRecognizer = new LinkedBlockingQueue<short[]>();
        synchronized (lock) { audioLength = 0; }
        running = true;
        ended = false;
        lastPartial = "";
        speechEndedSeen = false;
        record.startRecording();

        final AudioRecord rec = record;
        final LinkedBlockingQueue<short[]> queue = toRecognizer;
        readThread = new Thread(new Runnable()
        {
            @Override public void run()
            {
                short[] buf = new short[RATE / 50];   // 20 ms
                float floor = 0f;
                boolean floorKnown = false, speaking = false;
                int above = 0, quiet = 0, speech = 0;
                while (running)
                {
                    int n = rec.read(buf, 0, buf.length);
                    if (n <= 0) continue;

                    // The block's loudness, dB below full scale.
                    double sum = 0;
                    for (int i = 0; i < n; i++) sum += (double) buf[i] * buf[i];
                    float db = (float) (10 * Math.log10(sum / n / (32768.0 * 32768.0) + 1e-12));
                    if (!floorKnown) { floor = Math.max(-90f, db); floorKnown = true; }
                    if (!ended)
                    {
                        if (!speaking)
                        {
                            if (db < floor) floor = Math.max(-90f, db);
                            else if (db < floor + START_DB) floor += 0.06f;
                            above = db >= floor + START_DB ? above + 1 : 0;
                            if (above >= START_BLOCKS) { speaking = true; speech = above; quiet = 0; send("OnBridgeNote", "game heard speech start"); }
                        }
                        else
                        {
                            if (db <= floor + END_DB) quiet++; else { quiet = 0; speech++; }
                            if ((quiet >= END_BLOCKS && speech >= MIN_SPEECH_BLOCKS) || speech >= MAX_SPEECH_BLOCKS) endStream();
                            else if (quiet >= END_BLOCKS) { speaking = false; above = 0; }   // a click, not a word
                        }
                    }
                    synchronized (lock)
                    {
                        int room = audio.length - audioLength;
                        if (room > 0)
                        {
                            int k = Math.min(room, n);
                            System.arraycopy(buf, 0, audio, audioLength, k);
                            audioLength += k;
                        }
                    }
                    short[] copy = new short[n];
                    System.arraycopy(buf, 0, copy, 0, n);
                    queue.offer(copy);
                }
            }
        }, "salinlahi-mic");

        final OutputStream out = new ParcelFileDescriptor.AutoCloseOutputStream(writeEnd);
        writeThread = new Thread(new Runnable()
        {
            @Override public void run()
            {
                try
                {
                    while (running)
                    {
                        short[] block = queue.poll(100, java.util.concurrent.TimeUnit.MILLISECONDS);
                        if (block == null) continue;
                        if (block == END_OF_STREAM) break;   // closing the pipe tells the recognizer the answer is over
                        byte[] bytes = new byte[block.length * 2];
                        for (int i = 0; i < block.length; i++)
                        {
                            bytes[2 * i] = (byte) (block[i] & 0xff);
                            bytes[2 * i + 1] = (byte) ((block[i] >> 8) & 0xff);
                        }
                        out.write(bytes);
                    }
                }
                catch (Exception ignored) { /* the recognizer closed its end */ }
                finally { try { out.close(); } catch (IOException ignored) { } }
            }
        }, "salinlahi-pipe");
        readThread.start();
        writeThread.start();
    }

    /**
     * The child has finished: end the recognizer's audio, and if it has not
     * answered a second later, ask it to (stopListening).
     */
    private static void endStream()
    {
        if (ended) return;
        ended = true;
        LinkedBlockingQueue<short[]> q = toRecognizer;
        if (q != null) q.offer(END_OF_STREAM);
        send("OnBridgeNote", "game heard the end of the word - waiting for the recognizer");
        final SpeechRecognizer r = recognizer;
        new Handler(Looper.getMainLooper()).postDelayed(new Runnable()
        {
            @Override public void run()
            {
                if (r != null && r == recognizer)
                {
                    try { r.stopListening(); } catch (Throwable ignored) { }
                    send("OnBridgeNote", "asked the recognizer to finish");
                }
            }
        }, 600);
    }

    /** Keeps what was recorded as the answer, then stops everything. */
    private static void keepAnswer()
    {
        synchronized (lock)
        {
            float[] a = new float[audioLength];
            for (int i = 0; i < audioLength; i++) a[i] = audio[i] / 32768f;
            lastAnswer = a;
        }
    }

    private static void stopAll()
    {
        running = false;
        if (record != null)
        {
            try { record.stop(); } catch (Throwable ignored) { }
            try { record.release(); } catch (Throwable ignored) { }
            record = null;
        }
        if (recognizer != null)
        {
            try { recognizer.cancel(); } catch (Throwable ignored) { }
            try { recognizer.destroy(); } catch (Throwable ignored) { }
            recognizer = null;
        }
        try { if (readEnd != null) readEnd.close(); } catch (IOException ignored) { }
        readEnd = null;
        writeEnd = null;
        readThread = null;
        writeThread = null;
    }

    private static void send(String method, String message)
    {
        if (target != null) UnityPlayer.UnitySendMessage(target, method, message == null ? "" : message);
    }

    private static class Listener implements RecognitionListener
    {
        @Override public void onReadyForSpeech(Bundle params) { send("OnBridgeReady", ""); }
        @Override public void onBeginningOfSpeech() { send("OnBridgeSpeechBegan", ""); }
        @Override public void onRmsChanged(float rmsdB) { }
        @Override public void onBufferReceived(byte[] buffer) { }
        @Override public void onEndOfSpeech()
        {
            send("OnBridgeNote", "recognizer: end of speech");
            speechEndedSeen = true;
            // The recognizer has heard a word and says the speaker stopped:
            // finish now rather than wait for the game's own quiet detector.
            if (lastPartial.length() > 0) endStream();
        }
        @Override public void onPartialResults(Bundle partialResults)
        {
            ArrayList<String> texts = partialResults != null ? partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION) : null;
            if (texts != null && texts.size() > 0 && texts.get(0) != null && texts.get(0).trim().length() > 0)
            {
                lastPartial = texts.get(0).trim();
                send("OnBridgeNote", "partial: " + lastPartial);
                // A word is named and the recognizer already said the speech
                // ended (it can say so before the partial arrives): finish.
                if (speechEndedSeen) endStream();
            }
        }
        @Override public void onEvent(int eventType, Bundle params) { }

        @Override public void onError(int error)
        {
            // Some recognizers end a piped stream with "no match" even after a
            // partial result named the word: that partial is the answer.
            if ((error == SpeechRecognizer.ERROR_NO_MATCH || error == SpeechRecognizer.ERROR_SPEECH_TIMEOUT) && lastPartial.length() > 0)
            {
                Bundle b = new Bundle();
                ArrayList<String> list = new ArrayList<String>();
                list.add(lastPartial);
                b.putStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION, list);
                send("OnBridgeNote", "error " + error + " after a partial - using the partial");
                onResults(b);
                return;
            }
            keepAnswer();
            stopAll();
            send("OnBridgeError", Integer.toString(error));
        }

        @Override public void onResults(Bundle results)
        {
            keepAnswer();
            ArrayList<String> texts = results != null ? results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION) : null;
            String best = texts != null && texts.size() > 0 && texts.get(0) != null ? texts.get(0).trim() : "";
            if (best.length() == 0 && lastPartial.length() > 0)
            {
                best = lastPartial;
                send("OnBridgeNote", "final result empty - using the last partial \"" + best + "\"");
            }
            stopAll();
            send("OnBridgeResult", best);
        }
    }
}
