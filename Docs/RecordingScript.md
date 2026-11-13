# Recording script — Filipino words and cues

Generated from `Docs/VoiceOverScript.csv`. 49 words and 15 cue lines, all of which need a human voice.

## Why these and not the rest

The English narration is generated. These are not, because the model puts Filipino stress in the wrong place — six attempts could not make it say *tayo* (us) rather than *taYO* (get up) — and a cloned voice saying it wrong is worse than a stranger saying it wrong, because the child believes it.

## How to record

1. **One take, one file.** Record the whole list in a single pass; `Tools/split_word_recording.py` cuts it on the silences.
2. **Pause about a second between items.** That pause is what the splitter looks for. Shorter and two words merge into one clip.
3. **Say it two or three times if you fumble.** The splitter keeps the LAST take of each, so just repeat and move on — no need to restart.
4. **A phone is fine.** Quiet room, phone held a hand's width away, off the desk so it does not pick up knocks.
5. **Your normal teaching voice.** It does not need to sound like the generated lines — `Tools/voice_changer.py` re-renders it in Teacher Rachel's cloned voice afterwards and keeps your stress exactly as you said it.

Save to `Recordings/filipino_words.m4a`, then:

```
python3 Tools/split_word_recording.py Recordings/filipino_words.m4a
python3 Tools/voice_changer.py --words
```

---

## Part 1 — the 49 words

Say each word on its own, clearly, with a pause after it.

| # | Say this | Meaning |
|---|---|---|
| 1 | **Kaliwa** | Left |
| 2 | **Kanan** | Right |
| 3 | **Bukas** | Open |
| 4 | **Ilaw** | Light (turn on) |
| 5 | **Tuwalya** | Towel |
| 6 | **Sabon** | Soap |
| 7 | **Maligo** | Take a bath |
| 8 | **Sipilyo** | Toothbrush |
| 9 | **Suklay** | Comb |
| 10 | **Damit** | Clothes / uniform |
| 11 | **Medyas** | Socks |
| 12 | **Sapatos** | Shoes |
| 13 | **Bag** | Bag |
| 14 | **Kunin** | Take / get |
| 15 | **Upo** | Sit down |
| 16 | **Kanin** | Rice |
| 17 | **Itlog** | Egg |
| 18 | **Kain** | Eat |
| 19 | **Inom** | Drink |
| 20 | **Kumusta** | Hello / how are you |
| 21 | **Pasok** | Enter |
| 22 | **Takbo** | Run |
| 23 | **Kuha** | Get / take |
| 24 | **Lapis** | Pencil |
| 25 | **Aklat** | Book |
| 26 | **Basa** | Read |
| 27 | **Sulat** | Write |
| 28 | **Tanong** | Ask / question |
| 29 | **Sagot** | Answer |
| 30 | **Labas** | Go outside |
| 31 | **Laro** | Play |
| 32 | **Tawa** | Laugh |
| 33 | **Tulong** | Help |
| 34 | **Linis** | Clean up |
| 35 | **Alis** | Leave / go |
| 36 | **Paalam** | Goodbye |
| 37 | **Tingin** | Look |
| 38 | **Tuwa** | Joy / delight |
| 39 | **Akyat** | Climb |
| 40 | **Dausdos** | Slide |
| 41 | **Sakay** | Ride |
| 42 | **Tulak** | Push |
| 43 | **Talon** | Jump |
| 44 | **Ngiti** | Smile |
| 45 | **Bati** | Greet |
| 46 | **Ibahagi** | Share |
| 47 | **Salamat** | Thank you |
| 48 | **Pagod** | Tired |
| 49 | **Uwi** | Go home |

---

## Part 2 — the 15 cue lines

Whole sentences this time. Several mix Filipino and English in one breath — say them as one sentence, the way a teacher would, not as two halves.

| # | Say this | When the game uses it |
|---|---|---|
| 51 | Sabihin mo: | before a word she is asking for |
| 52 | Ngayon naman, sabihin mo: | before the second and later words |
| 53 | Pakinggan mo ulit: | when the child presses replay |
| 54 | Nakikinig ako. I am listening! | the microphone has opened |
| 55 | Pakinggan mong mabuti. Listen carefully. | a gentle hint |
| 56 | Press the blue button to hear the word again. | after a few wrong tries |
| 57 | Kaya mo 'yan! Go on, you can do it. | the child has gone quiet |
| 58 | Press the microphone button so Kylo can hear you. | the microphone is not on |
| 59 | Tama! That's right! | correct |
| 60 | Magaling! Well done! | correct |
| 61 | Ang galing mo! You did it! | correct |
| 62 | Hindi pa tama. Let's try that one again. | wrong word |
| 63 | Malapit na! You are very close. One more try. | wrong word, but close |
| 64 | Hindi kita narinig. I did not hear you - try once more. | nothing was heard |
| 65 | Pakilakasan mo ang boses mo. A little louder, please! | too quiet |

---

*Already recorded and not in this list: Bangon, Tayo, Lakad.*
