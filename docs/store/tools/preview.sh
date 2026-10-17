#!/bin/zsh
# Turn a simulator recording into an App Store app preview (App preview specifications):
# H.264 High at level 4.0, 30 fps, about 10 Mbps, a silent stereo AAC track, 15-30 s.
# usage: preview.sh <recording.mov> <out.mp4> <width> <height> [start seconds] [length seconds]
#   iPhone (also used for iPhone Duo): 886 1920    iPad 13": 1200 1600
set -e
in=$1; out=$2; w=$3; h=$4; start=${5:-0}; length=${6:-29.5}
mkdir -p ${out:h}
ffmpeg -v error -y -ss $start -t $length -i $in \
  -f lavfi -t $length -i anullsrc=channel_layout=stereo:sample_rate=48000 \
  -map 0:v -map 1:a \
  -vf "fps=30,scale=$w:$h:force_original_aspect_ratio=increase:flags=lanczos,crop=$w:$h,setsar=1" \
  -c:v libx264 -profile:v high -level:v 4.0 -pix_fmt yuv420p -b:v 10M -maxrate 12M -bufsize 20M \
  -c:a aac -b:a 256k -shortest -movflags +faststart $out
echo "$out $(( $(stat -f %z $out) / 1048576 )) MB"
