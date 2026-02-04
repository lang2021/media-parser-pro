"use client"

import { Video, ImageIcon } from "lucide-react"
import { cn } from "@/lib/utils"
import type { MediaFile } from "@/app/parse/page"

interface SelectedFileListProps {
  files: MediaFile[]
  selectedFile: MediaFile | null
  onSelectFile: (file: MediaFile) => void
}

export function SelectedFileList({ files, selectedFile, onSelectFile }: SelectedFileListProps) {
  const videoFiles = files.filter(f => f.type === "video")
  const imageFiles = files.filter(f => f.type === "image")

  return (
    <aside className="w-72 border-r border-border bg-card overflow-y-auto">
      <div className="p-3">
        <div className="flex items-center gap-2 mb-4 px-2">
          <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            Selected Files
          </span>
          <span className="text-xs text-muted-foreground">({files.length})</span>
        </div>

        {/* Video Files */}
        <div className="mb-4">
          <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2 px-2">
            Videos ({videoFiles.length})
          </h3>
          <div className="space-y-1">
            {videoFiles.map(file => (
              <FileItem
                key={file.id}
                file={file}
                isSelected={selectedFile?.id === file.id}
                onSelect={onSelectFile}
              />
            ))}
          </div>
        </div>

        {/* Image Files */}
        <div>
          <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2 px-2">
            Images ({imageFiles.length})
          </h3>
          <div className="space-y-1">
            {imageFiles.map(file => (
              <FileItem
                key={file.id}
                file={file}
                isSelected={selectedFile?.id === file.id}
                onSelect={onSelectFile}
              />
            ))}
          </div>
        </div>
      </div>
    </aside>
  )
}

interface FileItemProps {
  file: MediaFile
  isSelected: boolean
  onSelect: (file: MediaFile) => void
}

function FileItem({ file, isSelected, onSelect }: FileItemProps) {
  return (
    <div
      className={cn(
        "flex items-center gap-3 px-2 py-2 rounded-md cursor-pointer transition-colors",
        isSelected
          ? "bg-primary/10 border border-primary/20"
          : "hover:bg-muted border border-transparent"
      )}
      onClick={() => onSelect(file)}
    >
      <div className={cn(
        "w-8 h-8 rounded flex items-center justify-center flex-shrink-0",
        file.type === "video" ? "bg-blue-100 text-blue-600" : "bg-emerald-100 text-emerald-600"
      )}>
        {file.type === "video" ? (
          <Video className="w-4 h-4" />
        ) : (
          <ImageIcon className="w-4 h-4" />
        )}
      </div>
      <div className="flex-1 min-w-0">
        <span className={cn(
          "text-sm block truncate",
          isSelected ? "text-foreground font-medium" : "text-foreground"
        )}>
          {file.name}
        </span>
        {file.type === "video" && file.episodeNumber && (
          <span className="text-xs text-muted-foreground">Episode {file.episodeNumber}</span>
        )}
        {file.type === "image" && file.imageType && (
          <span className="text-xs text-muted-foreground capitalize">{file.imageType}</span>
        )}
      </div>
    </div>
  )
}
