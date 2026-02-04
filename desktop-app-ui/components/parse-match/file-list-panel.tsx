"use client"

import { cn } from "@/lib/utils"
import { Film, ImageIcon } from "lucide-react"
import type { MediaFile } from "@/app/parse-match/page"

interface FileListPanelProps {
  files: MediaFile[]
  selectedFile: MediaFile | null
  onSelectFile: (file: MediaFile) => void
}

export function FileListPanel({
  files,
  selectedFile,
  onSelectFile,
}: FileListPanelProps) {
  const videoFiles = files.filter(f => f.type === "video")
  const imageFiles = files.filter(f => f.type === "image")

  return (
    <div className="w-64 flex-shrink-0 border-r border-border bg-card flex flex-col overflow-hidden">
      <div className="p-3 border-b border-border">
        <h3 className="text-sm font-semibold text-foreground">Selected Files</h3>
        <p className="text-xs text-muted-foreground mt-0.5">{files.length} files selected</p>
      </div>

      <div className="flex-1 overflow-auto">
        {/* Video Files */}
        {videoFiles.length > 0 && (
          <div className="p-2">
            <div className="flex items-center gap-2 px-2 py-1.5">
              <Film className="h-3.5 w-3.5 text-primary" />
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Videos ({videoFiles.length})
              </span>
            </div>
            <div className="space-y-0.5">
              {videoFiles.map((file) => (
                <FileItem
                  key={file.id}
                  file={file}
                  isSelected={selectedFile?.id === file.id}
                  onSelect={() => onSelectFile(file)}
                />
              ))}
            </div>
          </div>
        )}

        {/* Image Files */}
        {imageFiles.length > 0 && (
          <div className="p-2">
            <div className="flex items-center gap-2 px-2 py-1.5">
              <ImageIcon className="h-3.5 w-3.5 text-emerald-600" />
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Images ({imageFiles.length})
              </span>
            </div>
            <div className="space-y-0.5">
              {imageFiles.map((file) => (
                <FileItem
                  key={file.id}
                  file={file}
                  isSelected={selectedFile?.id === file.id}
                  onSelect={() => onSelectFile(file)}
                />
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

function FileItem({
  file,
  isSelected,
  onSelect,
}: {
  file: MediaFile
  isSelected: boolean
  onSelect: () => void
}) {
  return (
    <button
      type="button"
      onClick={onSelect}
      className={cn(
        "w-full flex items-center gap-2 px-2 py-2 rounded-md text-left transition-colors",
        isSelected
          ? "bg-primary/10 text-foreground"
          : "hover:bg-muted text-foreground"
      )}
    >
      {file.type === "video" ? (
        <Film className="h-4 w-4 text-primary flex-shrink-0" />
      ) : (
        <ImageIcon className="h-4 w-4 text-emerald-600 flex-shrink-0" />
      )}
      <span className="text-sm truncate">{file.name}</span>
    </button>
  )
}
