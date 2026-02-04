"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { FileText, Sparkles } from "lucide-react"
import type { SeriesInfo, EpisodeInfo } from "@/app/parse-match/page"

interface MetadataPanelProps {
  seriesInfo: SeriesInfo
  episodes: EpisodeInfo[]
  currentEpisodeIndex: number
  onSeriesInfoChange: (field: keyof SeriesInfo, value: string) => void
  onEpisodeInfoChange: (field: keyof EpisodeInfo, value: string) => void
  onEpisodeSelect: (index: number) => void
}

export function MetadataPanel({
  seriesInfo,
  episodes,
  currentEpisodeIndex,
  onSeriesInfoChange,
  onEpisodeInfoChange,
  onEpisodeSelect,
}: MetadataPanelProps) {
  const [metadataText, setMetadataText] = useState("")
  const [isParsed, setIsParsed] = useState(false)

  const handleParse = () => {
    // Simulate parsing - in real app this would parse the text
    if (metadataText.trim()) {
      onSeriesInfoChange("title", "Sample Series Title")
      onSeriesInfoChange("originalTitle", "Original Title")
      onSeriesInfoChange("year", "2024")
      onSeriesInfoChange("studio", "Sample Studio")
      onSeriesInfoChange("director", "John Director")
      onSeriesInfoChange("actors", "Actor One, Actor Two, Actor Three")
      onSeriesInfoChange("tags", "Drama, Action, Thriller")
      setIsParsed(true)
    }
  }

  const currentEpisode = episodes[currentEpisodeIndex]

  return (
    <div className="w-80 flex-shrink-0 border-r border-border bg-card flex flex-col overflow-hidden">
      {/* Metadata Input */}
      <div className="p-4 border-b border-border">
        <Label className="text-sm font-medium text-foreground mb-2 block">
          Paste Metadata Text
        </Label>
        <Textarea
          value={metadataText}
          onChange={(e) => setMetadataText(e.target.value)}
          placeholder="Paste metadata text here..."
          className="min-h-32 resize-none text-sm"
        />
        <Button 
          onClick={handleParse} 
          className="w-full mt-3 gap-2"
          disabled={!metadataText.trim()}
        >
          <Sparkles className="h-4 w-4" />
          Parse
        </Button>
      </div>

      {/* Parse Result */}
      <div className="flex-1 overflow-auto">
        <div className="p-4 border-b border-border">
          <div className="flex items-center gap-2 mb-3">
            <FileText className="h-4 w-4 text-primary" />
            <h3 className="text-sm font-semibold text-foreground">Series Information</h3>
          </div>
          <div className="space-y-3">
            <FieldInput 
              label="Title" 
              value={seriesInfo.title} 
              onChange={(v) => onSeriesInfoChange("title", v)} 
            />
            <FieldInput 
              label="Original Title" 
              value={seriesInfo.originalTitle} 
              onChange={(v) => onSeriesInfoChange("originalTitle", v)} 
            />
            <FieldInput 
              label="Year" 
              value={seriesInfo.year} 
              onChange={(v) => onSeriesInfoChange("year", v)} 
            />
            <FieldInput 
              label="Studio" 
              value={seriesInfo.studio} 
              onChange={(v) => onSeriesInfoChange("studio", v)} 
            />
            <FieldInput 
              label="Director" 
              value={seriesInfo.director} 
              onChange={(v) => onSeriesInfoChange("director", v)} 
            />
            <FieldInput 
              label="Actors" 
              value={seriesInfo.actors} 
              onChange={(v) => onSeriesInfoChange("actors", v)} 
            />
            <FieldInput 
              label="Tags" 
              value={seriesInfo.tags} 
              onChange={(v) => onSeriesInfoChange("tags", v)} 
            />
          </div>
        </div>

        <div className="p-4">
          <div className="flex items-center gap-2 mb-3">
            <FileText className="h-4 w-4 text-primary" />
            <h3 className="text-sm font-semibold text-foreground">Episode Information</h3>
          </div>
          
          <div className="mb-3">
            <Label className="text-xs text-muted-foreground mb-1 block">Current Episode</Label>
            <Select 
              value={String(currentEpisodeIndex)} 
              onValueChange={(v) => onEpisodeSelect(Number(v))}
            >
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {episodes.map((ep, idx) => (
                  <SelectItem key={idx} value={String(idx)}>
                    Episode {ep.episodeNumber}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-3">
            <FieldInput 
              label="Episode Number" 
              value={currentEpisode.episodeNumber} 
              onChange={(v) => onEpisodeInfoChange("episodeNumber", v)} 
            />
            <FieldInput 
              label="Episode Title" 
              value={currentEpisode.episodeTitle} 
              onChange={(v) => onEpisodeInfoChange("episodeTitle", v)} 
            />
            <div>
              <Label className="text-xs text-muted-foreground mb-1 block">
                Episode Description
              </Label>
              <Textarea
                value={currentEpisode.episodeDescription}
                onChange={(e) => onEpisodeInfoChange("episodeDescription", e.target.value)}
                placeholder="Enter description..."
                className="min-h-16 resize-none text-sm"
              />
            </div>
            <FieldInput 
              label="Air Year" 
              value={currentEpisode.airYear} 
              onChange={(v) => onEpisodeInfoChange("airYear", v)} 
            />
            <FieldInput 
              label="Tags" 
              value={currentEpisode.tags} 
              onChange={(v) => onEpisodeInfoChange("tags", v)} 
            />
          </div>
        </div>
      </div>
    </div>
  )
}

function FieldInput({ 
  label, 
  value, 
  onChange 
}: { 
  label: string
  value: string
  onChange: (value: string) => void 
}) {
  return (
    <div>
      <Label className="text-xs text-muted-foreground mb-1 block">{label}</Label>
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={`Enter ${label.toLowerCase()}...`}
        className="h-8 text-sm"
      />
    </div>
  )
}
