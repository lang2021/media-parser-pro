"use client"

import { FolderOpen, Sparkles } from "lucide-react"
import { Button } from "@/components/ui/button"

interface ActionBarProps {
  onSelectFolder: () => void
  onParseGenerate: () => void
  folderPath: string | null
}

export function ActionBar({ onSelectFolder, onParseGenerate, folderPath }: ActionBarProps) {
  return (
    <header className="flex items-center justify-between px-4 py-3 border-b border-border bg-card">
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-md bg-primary flex items-center justify-center">
            <Sparkles className="w-4 h-4 text-primary-foreground" />
          </div>
          <span className="font-semibold text-foreground">Media Manager</span>
        </div>
        {folderPath && (
          <div className="text-sm text-muted-foreground border-l border-border pl-4">
            {folderPath}
          </div>
        )}
      </div>
      <div className="flex items-center gap-2">
        <Button 
          variant="outline" 
          size="sm"
          onClick={onSelectFolder}
          className="gap-2 bg-transparent"
        >
          <FolderOpen className="w-4 h-4" />
          Select Folder
        </Button>
        <Button 
          size="sm"
          onClick={onParseGenerate}
          className="gap-2"
        >
          <Sparkles className="w-4 h-4" />
          Parse & Generate
        </Button>
      </div>
    </header>
  )
}
