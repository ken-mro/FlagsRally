// Writes one line of speech to an audio file with a system voice, including the Siri voices (e.g. Sakura) that `say` cannot use.
// usage: swift speak.swift <voice identifier> <out.caf> <text>
//        swift speak.swift --list [language prefix, e.g. ja or en-US]    name, identifier, language, quality (2 = Enhanced/Premium)
// Run it with the `swift` interpreter: a compiled binary does not see the Siri voices in AVSpeechSynthesisVoice.speechVoices().
import AVFoundation

let a = CommandLine.arguments
if a.count > 1 && a[1] == "--list" {
    for v in AVSpeechSynthesisVoice.speechVoices() where a.count < 3 || v.language.hasPrefix(a[2]) {
        print([v.name, v.identifier, v.language, String(v.quality.rawValue)].joined(separator: "\t"))
    }
    exit(0)
}
guard let voice = AVSpeechSynthesisVoice.speechVoices().first(where: { $0.identifier == a[1] }) ?? AVSpeechSynthesisVoice(identifier: a[1]) else {
    fputs("voice not installed: \(a[1])\n", stderr); exit(2)
}
let synth = AVSpeechSynthesizer()
let u = AVSpeechUtterance(string: a[3])
u.voice = voice
var file: AVAudioFile?
var frames: AVAudioFrameCount = 0
var finished = false
synth.write(u) { buffer in
    guard let pcm = buffer as? AVAudioPCMBuffer else { return }
    if pcm.frameLength == 0 { finished = true; return }
    do {
        if file == nil {
            file = try AVAudioFile(forWriting: URL(fileURLWithPath: a[2]), settings: pcm.format.settings,
                                   commonFormat: pcm.format.commonFormat, interleaved: pcm.format.isInterleaved)
        }
        try file!.write(from: pcm)
        frames += pcm.frameLength
    } catch { fputs("\(error)\n", stderr); exit(3) }
}
while !finished { RunLoop.current.run(until: Date().addingTimeInterval(0.05)) }
if frames == 0 { fputs("no audio\n", stderr); exit(4) }
