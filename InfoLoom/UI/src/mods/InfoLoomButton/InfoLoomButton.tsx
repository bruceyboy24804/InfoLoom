
import { Button, Tooltip } from "cs2/ui";
import { tool } from "cs2/bindings";
import { bindValue, trigger, useValue } from "cs2/api";
import mod from "mod.json";
import icon from "images/Infoloom.svg";
import { useCallback } from "react";

const selectedTool = useValue(tool.activeTool$);
const toggleTool = useCallback(() => console.log("Hey ho!: " + mod.id), [selectedTool.id]);

export const InfoLoomButton = (componentList: any): any => {
  return (
    <Button
          src={icon}
          variant="floating"
          onSelect={toggleTool}
        />
  );
};
