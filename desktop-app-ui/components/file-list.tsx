"use client"

import { Video, ImageIcon } from "lucide-react"
import { Checkbox } from "@/components/ui/checkbox"
import type { MediaFile } from "@/app/page"
import { cn } from "@/lib/utils"

interface FileListProps {
  files: MediaFile[]
  selectedFile: MediaFile | null
  onToggleCheck: (id: string) => void
  onSelectFile: (file: MediaFile) => void
}

export function FileList({ files, selectedFile, onToggleCheck, onSelectFile }: FileListProps) {
  const videoFiles = files.filter(f => f.type === "video")
  const imageFiles = files.filter(f => f.type === "image")

  return (
    <aside className="w-80 border-r border-border bg-card overflow-y-auto">
      <div className="p-3">
        <div className="mb-4">
          <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2 px-2">
            Video Files ({videoFiles.length})
          </h3>
          <div className="space-y-1">
            {videoFiles.map(file => (
              <FileItem 
                key={file.id}
                file={file}
                isSelected={selectedFile?.id === file.id}
                onToggleCheck={onToggleCheck}
                onSelect={onSelectFile}
              />
            ))}
          </div>
        </div>
        <div>
          <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2 px-2">
            Image Files ({imageFiles.length})
          </h3>
          <div className="space-y-1">
            {imageFiles.map(file => (
              <FileItem 
                key={file.id}
                file={file}
                isSelected={selectedFile?.id === file.id}
                onToggleCheck={onToggleCheck}
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
  onToggleCheck: (id: string) => void
  onSelect: (file: MediaFile) => void
}

function FileItem({ file, isSelected, onToggleCheck, onSelect }: FileItemProps) {
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
      <Checkbox 
        checked={file.checked}
        onCheckedChange={() => onToggleCheck(file.id)}
        onClick={(e) => e.stopPropagation()}
        className="data-[state=checked]:bg-primary data-[state=checked]:border-primary"
      />
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
      <span className={cn(
        "text-sm truncate",
        isSelected ? "text-foreground font-medium" : "text-foreground"
      )}>
        {file.name}
      </span>
    </div>
  )
}
