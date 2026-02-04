"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { FileText, Sparkles } from "lucide-react"
import type { SeriesInfo, EpisodeInfo } from "@/app/parse/page"

interface MetadataEditorProps {
  metadataText: string
  onMetadataTextChange: (value: string) => void
  onParse: () => void
  isParsed: boolean
  seriesInfo: SeriesInfo
  onSeriesChange: (field: keyof SeriesInfo, value: string) => void
  episodes: EpisodeInfo[]
  currentEpisodeIndex: number
  onCurrentEpisodeChange: (index: number) => void
  onEpisodeChange: (field: keyof EpisodeInfo, value: string) => void
}

export function MetadataEditor({
  metadataText,
  onMetadataTextChange,
  onParse,
  isParsed,
  seriesInfo,
  onSeriesChange,
  episodes,
  currentEpisodeIndex,
  onCurrentEpisodeChange,
  onEpisodeChange,
}: MetadataEditorProps) {
  const currentEpisode = episodes[currentEpisodeIndex]

  return (
    <aside className="w-96 border-r border-border bg-card flex flex-col overflow-hidden">
      {/* Metadata Input Area */}
      <div className="p-4 border-b border-border">
        <div className="flex items-center gap-2 mb-3">
          <FileText className="w-4 h-4 text-muted-foreground" />
          <Label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            Metadata Text
          </Label>
        </div>
        <Textarea
          placeholder="Paste metadata text here..."
          value={metadataText}
          onChange={(e) => onMetadataTextChange(e.target.value)}
          className="min-h-32 resize-none text-sm"
        />
        <Button onClick={onParse} className="w-full mt-3 gap-2">
          <Sparkles className="w-4 h-4" />
          Parse
        </Button>
      </div>

      {/* Parse Results */}
      <div className="flex-1 overflow-y-auto p-4">
        <div className="flex items-center gap-2 mb-4">
          <div className={`w-2 h-2 rounded-full ${isParsed ? "bg-emerald-500" : "bg-muted-foreground/30"}`} />
          <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            Parse Result
          </span>
        </div>

        {/* Series Information */}
        <div className="mb-6">
          <h3 className="text-sm font-semibold text-foreground mb-3">Series Information</h3>
          <div className="space-y-3">
            <FieldInput label="Title" value={seriesInfo.title} onChange={(v) => onSeriesChange("title", v)} />
            <FieldInput label="Original Title" value={seriesInfo.originalTitle} onChange={(v) => onSeriesChange("originalTitle", v)} />
            <FieldInput label="Year" value={seriesInfo.year} onChange={(v) => onSeriesChange("year", v)} />
            <FieldInput label="Studio" value={seriesInfo.studio} onChange={(v) => onSeriesChange("studio", v)} />
            <FieldInput label="Director" value={seriesInfo.director} onChange={(v) => onSeriesChange("director", v)} />
            <FieldInput label="Actors" value={seriesInfo.actors} onChange={(v) => onSeriesChange("actors", v)} />
            <FieldInput label="Tags" value={seriesInfo.tags} onChange={(v) => onSeriesChange("tags", v)} />
          </div>
        </div>

        {/* Episode Information */}
        <div>
          <h3 className="text-sm font-semibold text-foreground mb-3">Episode Information</h3>
          <div className="mb-3">
            <Label className="text-xs text-muted-foreground mb-1.5 block">Current Episode</Label>
            <Select
              value={currentEpisodeIndex.toString()}
              onValueChange={(v) => onCurrentEpisodeChange(parseInt(v))}
            >
              <SelectTrigger className="text-sm">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {episodes.map((ep, i) => (
                  <SelectItem key={i} value={i.toString()}>
                    Episode {ep.episodeNumber}{ep.episodeTitle ? ` - ${ep.episodeTitle}` : ""}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-3">
            <FieldInput label="Episode Number" value={currentEpisode.episodeNumber} onChange={(v) => onEpisodeChange("episodeNumber", v)} />
            <FieldInput label="Episode Title" value={currentEpisode.episodeTitle} onChange={(v) => onEpisodeChange("episodeTitle", v)} />
            <div>
              <Label className="text-xs text-muted-foreground mb-1.5 block">Episode Description</Label>
              <Textarea
                value={currentEpisode.episodeDescription}
                onChange={(e) => onEpisodeChange("episodeDescription", e.target.value)}
                className="text-sm min-h-16 resize-none"
                placeholder="Enter description..."
              />
            </div>
            <FieldInput label="Air Year" value={currentEpisode.airYear} onChange={(v) => onEpisodeChange("airYear", v)} />
            <FieldInput label="Tags" value={currentEpisode.tags} onChange={(v) => onEpisodeChange("tags", v)} />
          </div>
        </div>
      </div>
    </aside>
  )
}

interface FieldInputProps {
  label: string
  value: string
  onChange: (value: string) => void
}

function FieldInput({ label, value, onChange }: FieldInputProps) {
  return (
    <div>
      <Label className="text-xs text-muted-foreground mb-1.5 block">{label}</Label>
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="text-sm h-9"
        placeholder={`Enter ${label.toLowerCase()}...`}
      />
    </div>
  )
}
