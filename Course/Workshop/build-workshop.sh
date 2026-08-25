#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Build a workshop/starter repository from the completed answer repository.

Usage:
  Course/Workshop/build-workshop.sh --lessons <spec> --output <directory>

Options:
  --lessons <spec>     Comma-separated lessons and ranges, e.g. 1,2,5-7 or 1-11.
  --output <directory> Destination directory. This may be the working tree of a
                       separate Git repository; its .git directory is preserved.
  --help               Show this help.

Examples:
  Course/Workshop/build-workshop.sh --lessons 1-2 --output ../AI.Business.Playground.Workshop
  Course/Workshop/build-workshop.sh --lessons 1,2,5-7 --output /tmp/ai-workshop

The output is generated from the current answer working tree. Existing top-level
LessonXX.* directories in the output are replaced. Other files and the output
repository's .git directory are left alone.
EOF
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PATCH_DIR="$SCRIPT_DIR/Patches"

LESSON_SPEC=""
OUTPUT_DIR=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --lessons)
      [[ $# -ge 2 ]] || { echo "Missing value for --lessons" >&2; exit 2; }
      LESSON_SPEC="$2"
      shift 2
      ;;
    --output)
      [[ $# -ge 2 ]] || { echo "Missing value for --output" >&2; exit 2; }
      OUTPUT_DIR="$2"
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

[[ -n "$LESSON_SPEC" ]] || { echo "--lessons is required" >&2; usage >&2; exit 2; }
[[ -n "$OUTPUT_DIR" ]] || { echo "--output is required" >&2; usage >&2; exit 2; }

mkdir -p "$OUTPUT_DIR"
OUTPUT_DIR="$(cd "$OUTPUT_DIR" && pwd)"

if [[ "$OUTPUT_DIR" == "$REPO_ROOT" ]]; then
  echo "Refusing to generate the workshop over the answer repository." >&2
  exit 2
fi

lesson_directory() {
  case "$1" in
    1)  echo "Lesson01.BasicPrompting" ;;
    2)  echo "Lesson02.ControllingLlmBehavior" ;;
    3)  echo "Lesson03.LlmConversations" ;;
    4)  echo "Lesson04.StructuredOutputs" ;;
    5)  echo "Lesson05.McpFundamentals" ;;
    6)  echo "Lesson06.ConsumingMcpServers" ;;
    7)  echo "Lesson07.RetrievalAugmentedGeneration" ;;
    8)  echo "Lesson08.SafeWriteOperations" ;;
    9)  echo "Lesson09.Agents" ;;
    10) echo "Lesson10.MonitoringAndAnomalyDetection" ;;
    11) echo "Lesson11.ProductionAiPlatform" ;;
    *) return 1 ;;
  esac
}

# Indexed arrays keep this compatible with the Bash 3.2 that ships with macOS.
SEEN=(0 0 0 0 0 0 0 0 0 0 0 0)
SELECTED=()
IFS=',' read -r -a PARTS <<< "$LESSON_SPEC"

for raw_part in "${PARTS[@]}"; do
  part="${raw_part//[[:space:]]/}"
  [[ -n "$part" ]] || continue

  if [[ "$part" =~ ^([0-9]+)-([0-9]+)$ ]]; then
    start="${BASH_REMATCH[1]}"
    end="${BASH_REMATCH[2]}"
    (( start <= end )) || { echo "Invalid lesson range: $part" >&2; exit 2; }

    for ((lesson = start; lesson <= end; lesson++)); do
      lesson_directory "$lesson" >/dev/null || { echo "Unknown lesson: $lesson" >&2; exit 2; }
      SEEN[$lesson]=1
    done
  elif [[ "$part" =~ ^[0-9]+$ ]]; then
    lesson="$part"
    lesson_directory "$lesson" >/dev/null || { echo "Unknown lesson: $lesson" >&2; exit 2; }
    SEEN[$lesson]=1
  else
    echo "Invalid lesson selection: $part" >&2
    exit 2
  fi
done

for lesson in {1..11}; do
  if [[ "${SEEN[$lesson]}" == "1" ]]; then
    SELECTED+=("$lesson")
  fi
done

[[ ${#SELECTED[@]} -gt 0 ]] || { echo "No lessons selected." >&2; exit 2; }

if command -v git >/dev/null 2>&1 && [[ -d "$REPO_ROOT/.git" ]]; then
  if [[ -n "$(git -C "$REPO_ROOT" status --porcelain --untracked-files=no)" ]]; then
    echo "Warning: the answer repository has tracked working-tree changes." >&2
    echo "The workshop will be generated from those current files, not strictly from HEAD." >&2
  fi
fi

# Replace only top-level lesson directories in the explicitly supplied output directory.
for existing_dir in "$OUTPUT_DIR"/Lesson[0-9][0-9].*; do
  [[ -d "$existing_dir" ]] || continue
  rm -rf -- "$existing_dir"
done

copy_lesson() {
  local source_dir="$1"
  local destination_dir="$2"

  if command -v rsync >/dev/null 2>&1; then
    rsync -a \
      --exclude '.DS_Store' \
      --exclude 'bin/' \
      --exclude 'obj/' \
      "$source_dir/" "$destination_dir/"
  else
    mkdir -p "$destination_dir"
    cp -R "$source_dir/." "$destination_dir/"
    find "$destination_dir" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
    find "$destination_dir" -name '.DS_Store' -delete
  fi
}

echo "Generating workshop in: $OUTPUT_DIR"
echo "Selected lessons: ${SELECTED[*]}"

for lesson in "${SELECTED[@]}"; do
  lesson_dir="$(lesson_directory "$lesson")"
  source_dir="$REPO_ROOT/$lesson_dir"
  destination_dir="$OUTPUT_DIR/$lesson_dir"

  [[ -d "$source_dir" ]] || { echo "Missing source lesson: $source_dir" >&2; exit 1; }

  echo "Copying $lesson_dir"
  copy_lesson "$source_dir" "$destination_dir"
done

if [[ -f "$REPO_ROOT/README.md" ]]; then
  cp "$REPO_ROOT/README.md" "$OUTPUT_DIR/README.md"
fi

for lesson in "${SELECTED[@]}"; do
  printf -v patch_name 'Lesson%02d.patch' "$lesson"
  patch_file="$PATCH_DIR/$patch_name"

  if [[ ! -f "$patch_file" ]]; then
    echo "No starter patch for Lesson$(printf '%02d' "$lesson"); using answer code as the additive starting point."
    continue
  fi

  [[ -s "$patch_file" ]] || continue

  echo "Applying $patch_name"
  (
    cd "$OUTPUT_DIR"
    git apply --check "$patch_file"
    git apply "$patch_file"
  )
done

{
  echo "# Generated workshop"
  echo
  echo "Generated from: $REPO_ROOT"
  echo "Lessons: ${SELECTED[*]}"
  echo
  echo "Regenerate this working tree from the answer repository instead of manually maintaining duplicated lesson code."
} > "$OUTPUT_DIR/.workshop-generation.md"

cat <<EOF

Workshop generation complete.

Output: $OUTPUT_DIR
Lessons: ${SELECTED[*]}

If $OUTPUT_DIR is a checkout of your workshop repository, review the changes there and commit/push them normally:

  git -C "$OUTPUT_DIR" status
  git -C "$OUTPUT_DIR" add .
  git -C "$OUTPUT_DIR" commit -m "Publish workshop lessons $LESSON_SPEC"
  git -C "$OUTPUT_DIR" push
EOF
